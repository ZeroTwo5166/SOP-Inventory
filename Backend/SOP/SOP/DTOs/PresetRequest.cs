namespace SOP.DTOs
{
    public class PresetRequest
    {

        [Required]
        public string Name { get; set; }

        [Required]
        public string Data { get; set; }


    }
}
