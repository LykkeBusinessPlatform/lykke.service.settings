using System.IO;
using System.Text;
using Common;

namespace Core.Models
{
    public struct BlobResult
    {
        private readonly MemoryStream _stream;

        public BlobResult(MemoryStream stream)
        {
            _stream = stream;
            _stream.Position = 0;
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
