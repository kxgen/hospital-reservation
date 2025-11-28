using System.Collections.Generic;
using Backend.Models;

namespace Backend.Data
{
    public static class InMemoryStore {
        public static List <User> Users = new List<User>
        {
            new User("hello@gmail.com", "123", "user"),
            new User("kibru@gmail.com", "123", "admin")
        };
    }
}