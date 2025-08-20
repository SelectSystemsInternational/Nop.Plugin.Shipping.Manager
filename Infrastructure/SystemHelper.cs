using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;

using Nop.Core.Domain.Security;
using Nop.Core.Infrastructure;

namespace Nop.Plugin.Apollo.Integrator
{
    /// <summary>
    /// Represents Mollie helper
    /// </summary>
    public class SystemHelper
    {

        #region Constants

        /// <summary>
        /// Represents plugin access mode
        /// </summary>
        public enum AccessMode
        {
            /// <summary>
            /// Testing
            /// </summary>
            NoAccess = 0,

            /// <summary>
            /// Testing
            /// </summary>
            Testing = 1,

            /// <summary>
            /// Configure 
            /// </summary>
            Configure = 2,

            /// <summary>
            /// Supervisor 
            /// </summary>
            Supervisor = 3,

            /// <summary>
            /// Manage 
            /// </summary>
            Manage = 4,

            /// <summary>
            /// Operate 
            /// </summary>
            Operate = 5

        }

        /// <summary>
        /// Represents plugin system mode
        /// </summary>
        public enum SystemMode
        {
            /// <summary>
            /// Testing
            /// </summary>
            NoAccess = 0,

            /// <summary>
            /// Testing
            /// </summary>
            Accommodation = 1,

            /// <summary>
            /// Bookings 
            /// </summary>
            Booking = 2,

            /// <summary>
            /// Appointments 
            /// </summary>
            Appointment = 3,

            /// <summary>
            /// Rentals 
            /// </summary>
            Rental = 4,

            /// <summary>
            /// Order Workflow 
            /// </summary>
            OrderWorkflow = 5,

            /// <summary>
            /// System 
            /// </summary>
            System = 9
        }

        /// <summary>
        /// Represents pluign record mode
        /// </summary>
        public enum RecordType
        {
            /// <summary>
            /// Testing
            /// </summary>
            AllTypes = 0,

            /// <summary>
            /// Testing
            /// </summary>
            Configuation = 1,

            /// <summary>
            /// Testing
            /// </summary>
            Setting = 2,
        }

        #endregion

        #region Methods

        PermissionRecord noAccess = new PermissionRecord
        {
            Name = "Admin area. No Access",
            SystemName = "NoAccess",
            Category = "Access"
        };

        PermissionRecord managePlugins = new PermissionRecord
        {
            Name = "Admin area.Manage Plugins",
            SystemName = "ManagePlugins",
            Category = "Configuration"
        };

        PermissionRecord mangeShippingSupervisor = new PermissionRecord
        {
            Name = "Admin area. Shipping Supervisor",
            SystemName = "ManageShippingSupervisor",
            Category = "ShippingManager"
        };

        PermissionRecord mangeShippingManager = new PermissionRecord
        {
            Name = "Admin area. Shipping Manager",
            SystemName = "ManageShippingManager",
            Category = "ShippingManager"
        };

        PermissionRecord operateShippingManager = new PermissionRecord
        {
            Name = "Admin area. Shipping Operator",
            SystemName = "OperateShippingManager",
            Category = "ShippingManager"
        };

        public PermissionRecord GetAccessPermission(AccessMode accessMode)
        {
            switch (accessMode)
            {
                case AccessMode.Testing:
                    return managePlugins;

                case AccessMode.Configure:
                    return managePlugins;

                case AccessMode.Supervisor:
                    return mangeShippingSupervisor;

                case AccessMode.Manage:
                    return mangeShippingManager;

                case AccessMode.Operate:
                    return operateShippingManager;
            }

            return noAccess;

        }

        public Guid DateToGuid(DateTime accessDate)
        {
            var date = new DateTime(accessDate.Year, accessDate.Month, accessDate.Day, accessDate.Hour, accessDate.Minute, 00);
            var guid = date.ToGuid();
            return guid;
        }

        public DateTime GuidToDate(Guid accessGuid)
        {
            var date = accessGuid.ToDateTime();
            return date;
        }

        public string GetDomainNameFromHost(string url)
        {
            string domain = string.Empty;
            if (url.Contains("localhost"))
                domain = "localhost";
            if (url != null)
            {
                string[] names = url.Split(".");
                for (int count = 1; count < names.Count(); count++)
                {
                    domain += names[count];
                    if (count + 1 < names.Count())
                        domain += ".";
                }
            }

            domain = domain.Replace("/", "");
            return domain;
        }

        #endregion

        public class CustomMetadataClass
        {
            public string PluginVersion = "";
            public string Shop = "";
            public string IpAddress = "";
            public string ShopRootUrl = "";
            public string ShopVersion = "";
            public string Partner = "";
        }

    }

    public static class StringExtension
    {
        public static string Truncate(this string s, int length)
        {
            return string.IsNullOrEmpty(s) || s.Length <= length ? s
                : length <= 0 ? string.Empty
                : s.Substring(0, length);
        }
    }

    public static class DateTimeExtensions
    {
        public static Guid ToGuid(this DateTime dt)
        {
            var bytes = BitConverter.GetBytes(dt.Ticks);

            Array.Resize(ref bytes, 16);

            return new Guid(bytes);
        }
    }

    public static class GuidExtensions
    {
        public static DateTime ToDateTime(this Guid guid)
        {
            var bytes = guid.ToByteArray();

            Array.Resize(ref bytes, 8);

            return new DateTime(BitConverter.ToInt64(bytes));
        }
    }

}

