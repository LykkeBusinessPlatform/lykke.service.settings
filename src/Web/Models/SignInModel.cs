using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class SignInModel
    {
        [Required]
        [DisplayName("E-mail")]
        public string Email { get; set; }

        [Required]
        [DisplayName("Password")]
        public string Password { get; set; }

        public string GoogleApiClientId { get; set; }
    }
}
