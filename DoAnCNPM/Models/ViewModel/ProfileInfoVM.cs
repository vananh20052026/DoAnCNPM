using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class ProfileInfoVM
    {
        [Required]
        [Display(Name = "Tên Đăng nhập")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required]
        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Số điện thoại")]
        public Nullable<int> Phone { get; set; } // hoặc int? cho ngắn gọn


        [Required]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Địa chỉ Email")]
        public string Email { get; set; }

        public string Role { get; set; } // Chức vụ

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; }


        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }
    }
}