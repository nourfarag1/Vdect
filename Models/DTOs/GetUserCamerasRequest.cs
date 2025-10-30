namespace Vedect.Models.DTOs
{
    public class GetUserCamerasRequest
    {
        public string CameraName { get; set; }
        public Guid Id { get; set; }
        public string StreamUrl { get; set; }
    }
}
