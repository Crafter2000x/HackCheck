﻿using Hack_Check.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Hack_Check.Classes
{
    public class AccountActions
    {
        public AccountViewModel FilledAccountViewModel(int UserId) 
        {
            Queries queries = new Queries();

            AccountViewModel FilledModel = queries.AccountInformation(UserId);

            return FilledModel;
        }

        public bool ServerSideValidationPassword(AccountViewModel accountViewModel) 
        {
            // Make sure non of the fields are empty
            if (accountViewModel.Password == null || accountViewModel.ConfirmPassword == null || accountViewModel.OldPassword == null || accountViewModel.Id <= 0 || accountViewModel.Username == null)
            {
                return false;
            }

            //Check if the passwords match
            if (accountViewModel.Password != accountViewModel.ConfirmPassword)
            {
                return false;
            }

            //Check for password requirments
            if (accountViewModel.Password.Length < 6 || accountViewModel.Password.Length > 100)
            {
                return false;
            }

            return true;
        }

        public bool ServerSideValidationUsername(AccountViewModel accountViewModel)
        {
            // Make sure non of the fields are empty
            if ( accountViewModel.OldPassword == null || accountViewModel.Id <= 0 || accountViewModel.Username == null || accountViewModel.NewUsername == null)
            {
                return false;
            }

            //Check for username requirments
            if (accountViewModel.NewUsername.Length < 5 || accountViewModel.NewUsername.Length > 24)
            {
                return false;
            }

            return true;
        }

        public bool CheckUsernameAvailable(AccountViewModel accountViewModel)
        {
            Queries queries = new Queries();
            if (queries.CheckUsernameAlreadyTaken(accountViewModel.NewUsername) == true)
            {
                return false;
            }
            return true;
        }

        public bool UpdateUsername(AccountViewModel accountViewModel)
        {
            Queries queries = new Queries();

            if (queries.UpdateUsernameInDatabase(accountViewModel) == true)
            {
                return true;
            }
            return false;
        }

        public bool CheckPassword(AccountViewModel accountViewModel) 
        {
            Queries queries = new Queries();
            
            accountViewModel.Salt = queries.RetrieveUserSalt(accountViewModel.Username);

            char firstletter = char.Parse(accountViewModel.Username.Substring(0, 1));
            int index = char.ToUpper(firstletter) - 64;

            // If is L or lower puts the salt in front of the password if higher the L puts it after, harder to brute force
            if (index <= 13)
            {
                accountViewModel.OldPassword = accountViewModel.Salt + accountViewModel.OldPassword;
            }
            else if (index > 13)
            {
                accountViewModel.OldPassword = accountViewModel.OldPassword + accountViewModel.Salt;
            }

            accountViewModel.OldPassword = ComputeStringToShHasa256Hash(accountViewModel.OldPassword);

            if (queries.MatchPasswords(accountViewModel.Username, accountViewModel.OldPassword))
            {
                return true;
            }

            return false;
        }

        public AccountViewModel SecurePassword(AccountViewModel accountViewModel) 
        {
            AccountViewModel DatabaseReadyAccountViewModel = accountViewModel;

            DatabaseReadyAccountViewModel.Salt = GetSalt();

            // Getting position in the alphabet of the first letter in the username and converting it to a index based on ASCII logic
            char firstletter = char.Parse(DatabaseReadyAccountViewModel.Username.Substring(0, 1));
            int index = char.ToUpper(firstletter) - 64;

            // If is L or lower puts the salt in front of the password if higher the L puts it after, harder to brute force
            if (index <= 13)
            {
                DatabaseReadyAccountViewModel.Password = DatabaseReadyAccountViewModel.Salt + DatabaseReadyAccountViewModel.Password;
            }
            else if (index > 13)
            {
                DatabaseReadyAccountViewModel.Password = DatabaseReadyAccountViewModel.Password + DatabaseReadyAccountViewModel.Salt;
            }

            DatabaseReadyAccountViewModel.Password = ComputeStringToShHasa256Hash(DatabaseReadyAccountViewModel.Password);

            return DatabaseReadyAccountViewModel;
        }

        public bool UpdatePassword(AccountViewModel accountViewModel) 
        {
            Queries queries = new Queries();

            return queries.UpdatePasswordInDatabase(accountViewModel);
        }

        public static string ComputeStringToShHasa256Hash(string plainText)
        {
            //Create a SHA256 hash from string   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                //Computing Hash - returns here byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(plainText));

                //now convert byte array to a string   
                StringBuilder stringbuilder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    stringbuilder.Append(bytes[i].ToString("x2"));
                }
                return stringbuilder.ToString();
            }
        }

        //Converts the salt into a string to use
        private static readonly int saltLengthLimit = 32;
        private static string GetSalt()
        {
            byte[] bytes = GetSalt(saltLengthLimit);
            StringBuilder stringbuilder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                stringbuilder.Append(bytes[i].ToString("x2"));
            }

            return stringbuilder.ToString();
        }

        //Generate a salt based on a max lenght and CryptoService
        private static byte[] GetSalt(int maximumSaltLength)
        {
            var salt = new byte[maximumSaltLength];
            using (var random = new RNGCryptoServiceProvider())
            {
                random.GetNonZeroBytes(salt);
            }
            return salt;
        }
    }
}