﻿using System;
using System.Collections.Generic;
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
    [MyApplicationService("UserStorageMaster")]
    public sealed class UserStorageServiceMaster : UserStorageServiceBase
    {
        #region Fields

        /// <summary>
        /// Services with slave mode.
        /// </summary>
        private readonly IList<IUserStorageService> _slaveServices;

        /// <summary>
        /// Collection of subcribers.
        /// </summary>
        private readonly IList<INotificationSubscriber> _subscribers;

        #endregion

        #region Constructors 

        /// <summary>
        /// Create an instance of <see cref="UserStorageServiceMaster"/>. 
        /// </summary>
        public UserStorageServiceMaster(
        IUserRepository repository = null,
        IUserValidator validator = null,
        IEnumerable<IUserStorageService> slaveServices = null)
            : base(repository, validator)
        {
            _slaveServices = slaveServices?.ToList() ?? new List<IUserStorageService>();

            _subscribers = new List<INotificationSubscriber>();

            Sender = new CompositeNotificationSender();
        }

        #endregion

        #region Events

        private event Action<User> UserAdded;

        private event Action<User> UserRemoved;

        #endregion

        #region Properties

        /// <summary>
        /// Mode of <see cref="UserStorageServiceMaster"/> work. 
        /// </summary>
        public override UserStorageServiceMode Mode => UserStorageServiceMode.MasterNode;

        /// <summary>
        /// Sender of notifications.
        /// </summary>
        public INotificationSender Sender { get; }

        #endregion

        #region Public methods

        #region Add

        /// <summary>
        /// Adds a new <see cref="User"/> to the storage.
        /// </summary>
        /// <param name="user">A new <see cref="User"/> that will be added to the storage.</param>
        public override void Add(User user)
        {
            base.Add(user);

            foreach (var ob in _subscribers)
            {
                ob.UserAdded(user);
            }

            foreach (var service in _slaveServices)
            {
                service.Add(user);
            }

            OnUserAdded(user);

            Sender.Send(new NotificationContainer
            {
                Notifications = new[]
                {
                    new Notification
                    {
                        Type = NotificationType.AddUser,
                        Action = new AddUserActionNotification
                        {
                            User = user
                        }
                    }
                }
            });
        }

        #endregion

        #region Remove

        /// <summary>
        /// Removes an existed <see cref="User"/> from the storage by id.
        /// </summary>
        public override void Remove(int id)
        {
            OnUserRemoved(FindFirst(x => x.Id == id));

            base.Remove(id);

            foreach (var ob in _subscribers)
            {
                ob.UserRemoved(FindFirst(x => x.Id == id));
            }

            foreach (var service in _slaveServices)
            {
                service.Remove(id);
            }

            Sender.Send(new NotificationContainer
            {
                Notifications = new[]
                {
                    new Notification
                    {
                        Type = NotificationType.DeleteUser,
                        Action = new DeleteUserActionNotification
                        {
                            UserId = id
                        }
                    }
                }
            });
        }

        #endregion

        #region Subscriber magement

        public void AddSubscriber(INotificationSubscriber subscriber)
        {
            if (subscriber == null)
            {
                throw new ArgumentNullException(nameof(subscriber));
            }

            _subscribers.Add(subscriber);
            UserAdded += subscriber.UserAdded;
            UserRemoved += subscriber.UserRemoved;
        }

        public void RemoveSubscriber(INotificationSubscriber subscriber)
        {
            if (subscriber == null)
            {
                throw new ArgumentNullException(nameof(subscriber));
            }

            _subscribers.Remove(subscriber);
            UserAdded -= subscriber.UserAdded;
            UserRemoved -= subscriber.UserRemoved;
        }

        #endregion

        #endregion

        #region Private methods

        private void OnUserAdded(User user)
        {
            UserAdded?.Invoke(user);
        }

        private void OnUserRemoved(User user)
        {
            UserRemoved?.Invoke(user);
        }

        #endregion
    }
}
