using System.IO;
using System.Text;
using Common;

namespace Core.Blob
{
    public struct AzureBlobResult
    {
        private readonly MemoryStream _stream;

        public string ETag { get; private set; }

        public AzureBlobResult(MemoryStream stream, string eTag)
        {
            _stream = stream;
            _stream.Position = 0;
            ETag = eTag;
        }

        public Stream AsStream()
        {

            return _stream;
        }

        public byte[] AsBytes()
        {
            return _stream.ToBytes();
        }

        public string AsString(Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            return encoding.GetString(AsBytes());
        }
    }
}
