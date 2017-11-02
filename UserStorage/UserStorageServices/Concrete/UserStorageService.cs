﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UserStorageServices.Abstract;
using UserStorageServices.Concrete.Validators;
using UserStorageServices.CustomExceptions;

namespace UserStorageServices.Concrete
{
    /// <summary>
    /// Represents a service that stores a set of <see cref="User"/>s and allows to search through them.
    /// </summary>
    public class UserStorageService : IUserStorageService
    {
        #region Fields

        /// <summary>
        /// Container for users.
        /// </summary>
        private readonly HashSet<User> _users;

        /// <summary>
        /// Generator of user id.
        /// </summary>
        private readonly IUserIdGenerator _userIdGenerator;

        /// <summary>
        /// Validator of user data.
        /// </summary>
        private readonly IValidator _validator;

        /// <summary>
        /// Returns true if logging is enabled.
        /// </summary>
        private readonly BooleanSwitch _logging = new BooleanSwitch("enableLogging", "Switch in config file");

        #endregion

        #region Constructors and properties

        /// <summary>
        /// Create an instance of <see cref="UserStorageService"/>. 
        /// </summary>
        public UserStorageService(IUserIdGenerator idGenerator, IValidator validator)
        {
            _users = new HashSet<User>();

            _userIdGenerator = idGenerator ?? new GuidUserIdGenerator();
            _validator = validator ?? new CompositeValidator();
        }

        /// <summary>
        /// Gets the number of elements contained in the storage.
        /// </summary>
        /// <returns>An amount of users in the storage.</returns>
        public int Count => _users.Count;

        #endregion

        #region Public methods

        #region Add

        /// <summary>
        /// Adds a new <see cref="User"/> to the storage.
        /// </summary>
        /// <param name="user">A new <see cref="User"/> that will be added to the storage.</param>
        public void Add(User user)
        {
            LogIfEnabled("Add method is called.");

            _validator.Validate(user);

            user.Id = _userIdGenerator.Generate();

            _users.Add(user);
        }

        /// <summary>
        /// Adds a new <see cref="User"/> to the storage.
        /// </summary>
        public void Add(string firstName, string lastName, int age)
        {
            Add(new User() { Age = age, FirstName = firstName, LastName = lastName });
        }

        #endregion

        #region Remove

        /// <summary>
        /// Removes an existed <see cref="User"/> from the storage by id.
        /// </summary>
        public void Remove(Guid id)
        {
            LogIfEnabled("Remove(Guid id) method is called.");

            int number = _users.RemoveWhere(x => x.Id == id);

            if (number == 0)
            {
                throw new UserNotFoundException("The user was not found");
            }
        }

        /// <summary>
        /// Removes an existed <see cref="User"/> from the storage.
        /// </summary>
        public void Remove(User user)
        {
            LogIfEnabled("Remove(User user) method is called.");

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            try
            {
                Remove(user.Id);
            }
            catch (UserNotFoundException exc)
            {
                throw new UserNotFoundException(exc.Message, user); // TODO:is it the right way
            }
        }

        #endregion

        #region Search

        #region returns IEnumerable<User>

        /// <summary>
        /// Searches through the storage for a <see cref="User"/> by first name.
        /// </summary>
        public IEnumerable<User> SearchByFirstName(string name)
        {
            LogIfEnabled("SearchByFirstName method is called.");

            CheckInputName(name);

            return Search(u => u.FirstName == name);
        }

        /// <summary>
        /// Searches through the storage for a <see cref="User"/> by last name.
        /// </summary>
        public IEnumerable<User> SearchByLastName(string name)
        {
            LogIfEnabled("SearchByLastName method is called.");

            CheckInputName(name);

            return Search(u => u.LastName == name);
        }

        /// <summary>
        /// Searches through the storage for a <see cref="User"/> by age.
        /// </summary>
        public IEnumerable<User> SearchByAge(int age)
        {
            LogIfEnabled("SearchByAge method is called.");

            if (age < 1 || age > 200)
            {
                throw new ArgumentException("Input age is incorrect");
            }

            return Search(u => u.Age == age);
        }

        /// <summary>
        /// Searches through the storage for a <see cref="User"/> by predicate.
        /// </summary>
        public IEnumerable<User> Search(Predicate<User> comparer)
        {
            LogIfEnabled("Search method is called.");

            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return _users.Select(u => u).Where(u => comparer(u));
        }

        #endregion

        #region returns User

        /// <summary>
        /// Searches through the storage for a <see cref="User"/> by the first name.
        /// </summary>
        public User FindFirstByFirstName(string name)
        {
            LogIfEnabled("FindFirstByFirstName method is called.");

            CheckInputName(name);

            return FindFirst(u => u.FirstName == name);
        }

        /// <summary>
        /// Searches through the storage for a <see cref="User"/> by last name.
        /// </summary>
        public User FindFirstByLastName(string name)
        {
            LogIfEnabled("FindFirstByLastName method is called.");

            CheckInputName(name);

            return FindFirst(u => u.LastName == name);
        }

        /// <summary>
        /// Searches through the storage for a <see cref="User"/> by age.
        /// </summary>
        public User FindFirstByAge(int age)
        {
            LogIfEnabled("FindFirstByAge method is called.");

            if (age < 1 || age > 200)
            {
                throw new ArgumentException("Input age is incorrect");
            }

            return FindFirst(u => u.Age == age);
        }

        /// <summary>
        /// Searches through the storage for a <see cref="User"/> by predicate.
        /// </summary>
        public User FindFirst(Predicate<User> comparer)
        {
            LogIfEnabled("FindFirst method is called.");

            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return _users.First(u => comparer(u));
        }

        #endregion

        #endregion

        #endregion

        #region Private methods

        private void CheckInputName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Input name is null or empty or whitespace", nameof(name));
            }
        }

        private void LogIfEnabled(string s)
        {
            if (_logging.Enabled)
            {
                Console.WriteLine(s);
            }
        }

        #endregion
    }
}
