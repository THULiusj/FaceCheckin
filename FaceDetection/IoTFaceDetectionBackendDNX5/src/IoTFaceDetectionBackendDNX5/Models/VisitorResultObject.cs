
namespace IoTFaceDetectionBackendDNX5.Models
{
    public class VisitorResultObject
    {
        public string status { get; set; }
        public int totalNum { get; set; }
        public int strangerNum { get; set; }
        public string[] visitorNames { get; set; }
    }
}