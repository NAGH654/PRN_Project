using Microsoft.AspNetCore.Http;
using Services.Service;
namespace API.Adapters
{
    public sealed class FormFileUpload : IUploadFile
    {
        private readonly IFormFile _inner;
        public FormFileUpload(IFormFile inner) => _inner = inner;

        public string FileName => _inner.FileName;
        public string ContentType => _inner.ContentType;
        public long Length => _inner.Length;
        public Stream OpenReadStream() => _inner.OpenReadStream();
    }
}
