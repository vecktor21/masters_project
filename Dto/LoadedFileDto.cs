using Diplom.Enums;

namespace Diplom.Dto
{
    public class LoadedFileDto
    {
        public DocKind Kind { get; set; }
        public byte[] Content { get; set; }
        public string Name { get; set; }
    }
}
