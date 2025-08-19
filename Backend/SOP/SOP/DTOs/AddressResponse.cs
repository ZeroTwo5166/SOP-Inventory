namespace SOP.DTOs
{
    public class AddressResponse
    {
        public int Id { get; set; }          // Expose PK to clients

        public int ZipCode { get; set; }

        public string Region { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Road { get; set; } = string.Empty;
    }
}
