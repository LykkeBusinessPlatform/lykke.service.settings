using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class SignInModel
    {
        [Required]
        [DisplayName("E-mail")]
        public string Email;

        [Required]
        [DisplayName("Password")]
        public string Password;

        public string GoogleApiClientId { get; set; }
    }
}
