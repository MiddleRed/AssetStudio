//! Read-path benchmark and byte-exactness harness.
//!
//! Opens each input bundle, lists every object, reads all objects through the
//! by-index direct retry path, and prints per-bundle timing plus an FNV-1a hash
//! over every item's metadata and payload bytes. Running the same inputs against
//! two library builds must produce identical hashes.
//!
//! Usage:
//!   bench <native-library> [--unity-version V] [--rounds N] [--kind K] <input>...

use std::time::Instant;

use haruki_assetstudio::{AssetStudioLibrary, ObjectReadByIndexRequest};

const FNV_OFFSET: u64 = 0xcbf29ce484222325;
const FNV_PRIME: u64 = 0x100000001b3;

fn fnv1a(hash: &mut u64, bytes: &[u8]) {
    for &byte in bytes {
        *hash ^= u64::from(byte);
        *hash = hash.wrapping_mul(FNV_PRIME);
    }
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut library_path = None;
    let mut unity_version = None;
    let mut rounds = 1usize;
    let mut kind = "auto".to_string();
    let mut inputs = Vec::new();

    let mut args = std::env::args().skip(1);
    while let Some(arg) = args.next() {
        match arg.as_str() {
            "--unity-version" => unity_version = Some(args.next().expect("--unity-version value")),
            "--rounds" => rounds = args.next().expect("--rounds value").parse()?,
            "--kind" => kind = args.next().expect("--kind value"),
            _ if library_path.is_none() => library_path = Some(arg),
            _ => inputs.push(arg),
        }
    }
    let library_path = library_path.expect("native library path");
    assert!(!inputs.is_empty(), "at least one input bundle required");

    let library = AssetStudioLibrary::load(&library_path)?;

    let mut grand_hash = FNV_OFFSET;
    let mut grand_bytes = 0u64;
    let mut grand_objects = 0u64;
    let started = Instant::now();

    for round in 0..rounds {
        for input in &inputs {
            let bundle_started = Instant::now();
            let context = library.open(input, unity_version.as_deref(), &[], false)?;

            let mut object_indexes = Vec::new();
            let mut offset = 0;
            loop {
                let page = context.list_objects(offset, 4096, &[])?;
                if page.is_empty() {
                    break;
                }
                offset += page.len() as i32;
                object_indexes.extend(page.iter().map(|asset| asset.index));
                if page.len() < 4096 {
                    break;
                }
            }

            let mut bundle_hash = FNV_OFFSET;
            let mut bundle_bytes = 0u64;
            for chunk in object_indexes.chunks(64) {
                let requests: Vec<ObjectReadByIndexRequest> = chunk
                    .iter()
                    .map(|&object_index| ObjectReadByIndexRequest {
                        object_index,
                        kind: &kind,
                        image_format: "raw_rgba",
                    })
                    .collect();
                let read = context.read_by_index_retry(&requests)?;
                for item in &read.items {
                    fnv1a(&mut bundle_hash, &item.path_id.to_le_bytes());
                    fnv1a(&mut bundle_hash, &item.status.to_le_bytes());
                    fnv1a(&mut bundle_hash, item.payload_kind.as_bytes());
                    fnv1a(&mut bundle_hash, item.suggested_extension.as_bytes());
                    if let Some(payload) = read.payload_for(item) {
                        fnv1a(&mut bundle_hash, &(payload.len() as u64).to_le_bytes());
                        fnv1a(&mut bundle_hash, payload);
                        bundle_bytes += payload.len() as u64;
                    }
                }
                grand_objects += read.items.len() as u64;
            }

            fnv1a(&mut grand_hash, &bundle_hash.to_le_bytes());
            grand_bytes += bundle_bytes;
            if round == 0 {
                println!(
                    "{input}: objects={} payload_bytes={} hash={bundle_hash:016x} elapsed_ms={}",
                    object_indexes.len(),
                    bundle_bytes,
                    bundle_started.elapsed().as_millis()
                );
            }
        }
    }

    println!(
        "TOTAL rounds={rounds} objects={grand_objects} payload_bytes={grand_bytes} hash={grand_hash:016x} elapsed_ms={}",
        started.elapsed().as_millis()
    );
    Ok(())
}
