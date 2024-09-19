﻿using System.ComponentModel.DataAnnotations;

namespace UserManager.DTOs
{
    public class LoginModel
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

}
