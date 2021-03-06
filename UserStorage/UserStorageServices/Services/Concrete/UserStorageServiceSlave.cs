﻿using System;
using System.Diagnostics;
using System.Linq;
using UserStorageServices.CustomAttributes;
using UserStorageServices.Enums;
using UserStorageServices.Notifications.Abstract;
using UserStorageServices.Notifications.Concrete;
using UserStorageServices.Repositories.Abstract;
using UserStorageServices.Services.Abstract;
using UserStorageServices.Validators.Abstract;

namespace UserStorageServices.Services.Concrete
{
    [Serializable]
    [MyApplicationService("UserStorageSlave")]
    public sealed class UserStorageServiceSlave : UserStorageServiceBase
    {
        #region Constructors and properties

        /// <summary>
        /// Create an instance of <see cref="UserStorageServiceSlave"/>. 
        /// </summary>
        public UserStorageServiceSlave(IUserRepository repository = null, IUserValidator validator = null)
            : base(repository, validator)
        {
            var receiver = new NotificationReceiver();
            receiver.Received += NotificationReceived;
            Receiver = receiver;
        }

        /// <summary>
        /// Mode of <see cref="UserStorageServiceSlave"/> work. 
        /// </summary>
        public override UserStorageServiceMode Mode => UserStorageServiceMode.SlaveNode;

        /// <summary>
        /// Notification receiver.
        /// </summary>
        public INotificationReceiver Receiver { get; }

        #endregion

        #region Public methods

        #region Add

        /// <summary>
        /// Adds a new <see cref="User"/> to the storage.
        /// </summary>
        /// <param name="user">A new <see cref="User"/> that will be added to the storage.</param>
        public override void Add(User user)
        {
            if (IsAvailable())
            {
                base.Add(user);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region Remove

        /// <summary>
        /// Removes an existed <see cref="User"/> from the storage by id.
        /// </summary>
        public override void Remove(int id)
        {
            if (IsAvailable())
            {
                base.Remove(id);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #endregion

        #region Private methods

        /// <summary>
        /// Check is method called from masterNode by reflection.
        /// </summary>
        private bool IsAvailable()
        {
            var trace = new StackTrace();

            var currentCalled = trace.GetFrame(1).GetMethod();
            var currentCalledParams = currentCalled.GetParameters();
            Type[] parTypes = new Type[currentCalledParams.Length];
            for (int i = 0; i < parTypes.Length; i++)
            {
                parTypes[i] = currentCalledParams[i].ParameterType;
            }

            var calledMethod = typeof(UserStorageServiceMaster).GetMethod(currentCalled.Name, parTypes);

            var flag = (trace.GetFrames() ?? throw new InvalidOperationException()).Select(x => x.GetMethod())
                .Contains(calledMethod);

            return flag;
        }

        private void NotificationReceived(NotificationContainer container)
        {
            foreach (var item in container.Notifications)
            {
                if (item.Type == NotificationType.AddUser)
                {
                    var user = ((AddUserActionNotification)item.Action).User;
                    Add(user);
                }
                else
                {
                    var userId = ((DeleteUserActionNotification)item.Action).UserId;
                    Remove(userId);
                }
            }
        }

        #endregion
    }
}
