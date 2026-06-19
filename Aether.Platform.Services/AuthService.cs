using System;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;
using Aether.Platform.Core.Utilities;

namespace Aether.Platform.Services
{
    public class AuthService : IAuthService
    {
        private readonly IIfmsBroker _ifmsBroker;
        private bool _isLoggedIn;
        private DateTime? _usbKeyActivatedTime;

        public bool IsLoggedIn => _isLoggedIn;

        public bool IsUSBKeyExpired
        {
            get
            {
                if (!_usbKeyActivatedTime.HasValue) return true;
                return DateTime.Now - _usbKeyActivatedTime.Value > USBKeyValidDuration;
            }
        }

        public TimeSpan USBKeyValidDuration => TimeSpan.FromDays(30);

        public AuthService()
        {
            try { _ifmsBroker = ServiceLocator.GetService<IIfmsBroker>(); }
            catch { _ifmsBroker = null; }
        }

        public Task<LoginResult> LoginAsync(string userId, string password)
        {
            var result = new LoginResult { Success = true, UserId = userId, UserName = userId, Role = UserRole.Technician };
            _isLoggedIn = true;
            return Task.FromResult(result);
        }

        public Task<LoginResult> LoginWithCardAsync(string cardId)
        {
            var result = new LoginResult { Success = true, UserId = cardId, UserName = $"Card_{cardId}", Role = UserRole.Operator };
            _isLoggedIn = true;
            return Task.FromResult(result);
        }

        public Task<LoginResult> LoginWithFingerprintAsync(byte[] data)
        {
            var result = new LoginResult { Success = false, ErrorMessage = "指纹模块未连接" };
            return Task.FromResult(result);
        }

        public Task<LoginResult> LoginWithFaceAsync(byte[] faceData)
        {
            var result = new LoginResult { Success = false, ErrorMessage = "人脸模块未连接" };
            return Task.FromResult(result);
        }

        public Task<LoginResult> LoginWithUSBKeyAsync()
        {
            if (IsUSBKeyExpired)
                return Task.FromResult(new LoginResult { Success = false, ErrorMessage = "USB Key 已过期" });

            var result = new LoginResult { Success = true, UserId = "USBKEY", UserName = "USB Key User", Role = UserRole.Administrator };
            _isLoggedIn = true;
            return Task.FromResult(result);
        }

        public bool ValidateDynamicPassword(string password) => true;

        public bool ValidateIFMSAccess(string userId, string deviceId)
        {
            if (_ifmsBroker != null && _ifmsBroker.IsEnabled)
            {
                try
                {
                    var task = _ifmsBroker.ValidatePartNumberAsync(userId, deviceId);
                    task.Wait(TimeSpan.FromSeconds(5));
                    return task.Result;
                }
                catch { return true; }
            }
            return true;
        }

        public bool ActivateUSBKey(string code)
        {
            _usbKeyActivatedTime = DateTime.Now;
            return true;
        }

        public void Logout() { _isLoggedIn = false; }
    }
}
