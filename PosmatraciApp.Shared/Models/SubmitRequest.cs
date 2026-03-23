namespace PosmatraciApp.Shared.Models
{
    public class SubmitRequest
    {
        public string Email { get; set; } = "";
        public BmState? BmState { get; set; }
        public BmIzlaznost? BmIzlaznost { get; set; }
    }
}
