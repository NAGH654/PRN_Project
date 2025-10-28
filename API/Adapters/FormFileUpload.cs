using Microsoft.AspNetCore.Http;
namespace API.Adapters
{
    // kept for future use; no interface dependency now
    public sealed class FormFileUpload
    {
        private readonly IFormFile _inner;
        public FormFileUpload(IFormFile inner) => _inner = inner;

        public string FileName => _inner.FileName;
        public string ContentType => _inner.ContentType;
        public long Length => _inner.Length;
        public Stream OpenReadStream() => _inner.OpenReadStream();
    }
}
