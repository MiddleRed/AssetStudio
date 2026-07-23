using System.Text.Json.Serialization;

namespace AssetStudio
{
    public class GLTextureSettings
    {
        public int m_FilterMode;
        public int m_Aniso;
        public float m_MipBias;
        public int m_WrapMode;
        [JsonInclude]
        public int m_WrapU
        {
            get => m_WrapMode;
            set => m_WrapMode = value;
        }
        public int m_WrapV;
        public int m_WrapW;

        public GLTextureSettings() { }

        public GLTextureSettings(ObjectReader reader)
        {
            var version = reader.version;

            m_FilterMode = reader.ReadInt32();
            m_Aniso = reader.ReadInt32();
            m_MipBias = reader.ReadSingle();
            if (version >= 2017)//2017.x and up
            {
                m_WrapU = reader.ReadInt32();
                m_WrapV = reader.ReadInt32();
                m_WrapW = reader.ReadInt32();
            }
            else
            {
                m_WrapU = reader.ReadInt32();
                m_WrapV = m_WrapU;
                m_WrapW = m_WrapU;
            }
        }
    }
}
