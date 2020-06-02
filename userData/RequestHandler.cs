using System;
using System.Collections.Generic;
using Messaging.Models;
using System.Linq;
using Newtonsoft.Json;
using UDMRequestClass;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Messaging
{

    // Currently, I am using RPC (remote procedure call) from rabbitmq to implment a reply and request system for communication. However, not every request needs a reply. For now, for requests that don't
    // need a reply I am simply replying with null. After the code review I will fix that so nI don't have to send a reply for every request. But for now this is how it has been implemented. 

    public static class RequestHandler
    {
        // Calls method that will perform the operation listed in the request. Valid operations include Insert, Read, Update, and Delete. 
        public static string MainHandler(UDMRequest obj)
        {
            switch (obj.Operation)
            {
                case UDMOperation.Insert:
                    return Insert(obj);
                case UDMOperation.Read:
                    return Read(obj);
                case UDMOperation.Update:
                    return Update(obj);
                case UDMOperation.Delete:
                    return Delete(obj);
            }
            return null;
        }

        // Handles all insert operations. Can insert a new user, portfolio, or holding. 
        public static string Insert(UDMRequest obj)
        {
            var context = new userDBContext();
            switch (obj.RequestType)
            {
                case UDMRequestType.User:
                    context.Add(ConvertToUser(obj.User));
                    context.SaveChanges();
                    break;
                case UDMRequestType.Portfolio:
                    foreach (UDMPortfolio p in obj.Portfolio)
                    {
                        context.Add(ConvertToPortfolio(p));
                    }
                    context.SaveChanges();
                    break;
                case UDMRequestType.Holding:
                    context.Add(ConvertToHolding(obj.Holding));
                    context.SaveChanges();
                    break;
            }
            string returnMessage = $"Performed {obj.Operation} on {obj.RequestType}";
            return returnMessage;
        }

        // Main read method that calls accesory read methods. Can read a user, portfolio, or porfoltio history.
        public static string Read(UDMRequest obj)
        {
            switch (obj.RequestType)
            {
                case UDMRequestType.User:
                    return ReadUser(obj);
                case UDMRequestType.Portfolio:
                    return ReadPortfolio(obj);
                case UDMRequestType.PortfolioHistory:
                    return ReadPortfolioHistory(obj);
            }
            string returnMessage = $"Performed {obj.Operation} on {obj.RequestType}";
            return returnMessage;
        }

        // Reads a user from the db
        public static string ReadUser(UDMRequest obj)
        {
            var context = new userDBContext();
            if (!(obj.Email is null)) // If user included an email in the request, read the user that the email belongs to. 
            {
                var user = context.User.FirstOrDefault(a => a.Email == obj.Email);
                return serialize(ConvertToUDMUser(user));
            }
            else // If request didn't include email, then read the user with the same ID as the ID specified by the objectID member of the UDMRequest class. 
            {
                var user = context.User.FirstOrDefault(a => a.Id == obj.objectID);
                return serialize(ConvertToUDMUser(user));
            }
        }

        // Reads a portfolio from the db
        public static string ReadPortfolio(UDMRequest obj)
        {
            var context = new userDBContext();
            if (!(obj.objectID is null)) // If user included the id of the portfolio they want to read, then we return just the portfolio with the same id. 
            {
                List<UDMPortfolio> portfolios = new List<UDMPortfolio>();
                var usersPortfolio = context.Portfolio.FirstOrDefault(a => a.Id == obj.objectID);
                usersPortfolio.Holding = context.Holding.Where(a => a.PortfolioId == usersPortfolio.Id).ToList();
                portfolios.Add(ConvertToUDMPortfolio(usersPortfolio));
                return serialize(portfolios);
            }
            if (!(obj.Email is null)) // If the user didn'd include the id of the portfolio, but included a user's email, then we return every portfolio that belongs to the user
            {
                List<UDMPortfolio> portfolios = new List<UDMPortfolio>();
                var selectUser = context.User.FirstOrDefault(a => a.Email == obj.Email);
                var usersPortfolios = context.Portfolio.Where(a => a.OwnerId == selectUser.Id).ToList();
                foreach (var p in usersPortfolios)
                {
                    p.Holding = context.Holding.Where(a => a.PortfolioId == p.Id).ToList();
                    portfolios.Add(ConvertToUDMPortfolio(p));
                }
                return serialize(portfolios);
            }
            else // If the request didn't have a portfolio id or an email then we return every portfolio that doesn't belong to anyone. 
            {
                List<UDMPortfolio> portfolios = new List<UDMPortfolio>();
                var unownedPortfolios = context.Portfolio.Where(a => a.OwnerId == null).ToList();
                foreach (var p in unownedPortfolios)
                {
                    p.Holding = context.Holding.Where(a => a.PortfolioId == p.Id).ToList();
                    portfolios.Add(ConvertToUDMPortfolio(p));
                }
                return serialize(portfolios);
            }
        }

        // Gets the portfolio history of a portfolio from the db
        public static string ReadPortfolioHistory(UDMRequest obj)
        {
            var context = new userDBContext();
            if (!(obj.objectID is null)) // If the request includes the id of the portfolio for which they want the history, then we just return the history of that single portfolio.
            {
                List<List<UDMPortfolioHistory>> portfolioHistory = new List<List<UDMPortfolioHistory>>();
                var history = context.PortfolioHistory.Where(a => a.PortfolioId == obj.objectID).ToList();
                portfolioHistory.Add(ConvertToUDMPortfolioHistory(history));
                return serialize(portfolioHistory);
            }
            if (!(obj.Email is null)) // If the request has no id but has an email, then we return the history of every portfolio the user owns. 
            {
                List<List<UDMPortfolioHistory>> allHistories = new List<List<UDMPortfolioHistory>>();
                var selectUser = context.User.FirstOrDefault(a => a.Email == obj.Email);
                var usersPortfolios = context.Portfolio.Where(a => a.OwnerId == selectUser.Id).ToList();
                foreach (var p in usersPortfolios)
                {
                    List<PortfolioHistory> singleHistory = new List<PortfolioHistory>();
                    singleHistory = context.PortfolioHistory.Where(a => a.PortfolioId == p.Id).ToList();
                    allHistories.Add(ConvertToUDMPortfolioHistory(singleHistory));
                }
                return serialize(allHistories);
            }
            else // If the request contains no id and no email, then we return the history of every portfolio that is not owned by anyone. 
            {
                List<List<UDMPortfolioHistory>> allHistories = new List<List<UDMPortfolioHistory>>();
                var unownedPortfolios = context.Portfolio.Where(a => a.OwnerId == null).ToList();
                foreach (var p in unownedPortfolios)
                {
                    List<PortfolioHistory> singleHistory = new List<PortfolioHistory>();
                    singleHistory = context.PortfolioHistory.Where(a => a.PortfolioId == p.Id).ToList();
                    allHistories.Add(ConvertToUDMPortfolioHistory(singleHistory));
                }
                return serialize(allHistories);
            }
        }

        // Main update method that calls accesory update methods. Can update a user, portfolio, or holding.
        public static string Update(UDMRequest obj)
        {
            switch (obj.RequestType)
            {
                case UDMRequestType.User:
                    UpdateUser(obj);
                    break;
                case UDMRequestType.Portfolio:
                    UpdatePortfolio(obj);
                    break;
                case UDMRequestType.Holding:
                    UpdateHolding(obj);
                    break;
            }
            string returnMessage = $"Performed {obj.Operation} on {obj.RequestType}";
            return returnMessage;
        }

        public static string UpdateUser(UDMRequest obj)
        {
            var context = new userDBContext();
            var user = (User)null;
            if (!(obj.Email is null)) // updates the user that either has the email specified in the request, or has the id specified in the request. 
            {
                user = context.User.FirstOrDefault(a => a.Email == obj.Email);
            }
            else
            {
                user = context.User.FirstOrDefault(a => a.Id == obj.objectID);
            }
            if (!(obj.User.Email is null)) // can only update the user's email, first name, last name, and brokerage account. 
            {
                user.Email = obj.User.Email;
            }
            if (!(obj.User.FirstName is null))
            {
                user.FirstName = obj.User.FirstName;
            }
            if (!(obj.User.LastName is null))
            {
                user.LastName = obj.User.LastName;
            }
            if (!(obj.User.BrokerageAccount is null))
            {
                user.BrokerageAccount = obj.User.BrokerageAccount;
            }
            context.SaveChanges();
            string returnMessage = $"Performed {obj.Operation} on {obj.RequestType}";
            return returnMessage;
        }

        public static string UpdatePortfolio(UDMRequest obj)
        {
            var context = new userDBContext();
            var Portfolio = context.Portfolio.FirstOrDefault(a => a.Id == obj.objectID);  // updates the portolio that has the id specified in the request. 
            if (!((int?)obj.Portfolio[0].OwnerId is null)) // can only update the portfolio's owner id, whether it is active or not, stop value, and desired risk. 
            {
                Portfolio.OwnerId = obj.Portfolio[0].OwnerId;
            }
            if (!((bool?)obj.Portfolio[0].Active is null))
            {
                Portfolio.Active = obj.Portfolio[0].Active;
            }
            if (!((decimal?)obj.Portfolio[0].StopValue is null))
            {
                Portfolio.StopValue = obj.Portfolio[0].StopValue;
            }
            if (!((int?)obj.Portfolio[0].DesiredRisk is null))
            {
                Portfolio.DesiredRisk = (int)obj.Portfolio[0].DesiredRisk;
            }
            context.SaveChanges();
            string returnMessage = $"Performed {obj.Operation} on {obj.RequestType}";
            return returnMessage;
        }

        public static string UpdateHolding(UDMRequest obj)
        {
            var context = new userDBContext();
            var holding = context.Holding.FirstOrDefault(a => a.Id == obj.objectID); // updates the holding that has the id specified in the request.                     
            if (!(obj.Holding.Description is null)) // can only update the holding's description and quantity. 
            {
                holding.Description = obj.Holding.Description;
            }
            if (!((int?)obj.Holding.Quantity is null))
            {
                holding.Quantity = obj.Holding.Quantity;
            }
            context.SaveChanges();
            string returnMessage = $"Performed {obj.Operation} on {obj.RequestType}";
            return returnMessage;
        }

        // Can delete a single user, portfolio, holding, or single portfolio history snapshot.  
        public static string Delete(UDMRequest obj)
        {
            var context = new userDBContext();
            switch (obj.RequestType)
            {
                case UDMRequestType.User:
                    if (!(obj.Email is null))
                    {
                        context.Remove(context.User.Single(a => a.Email == obj.Email));
                    }
                    else
                    {
                        context.Remove(context.User.Single(a => a.Id == obj.objectID));
                    } 
                    context.SaveChanges();
                    break;
                case UDMRequestType.Portfolio:
                    context.Remove(context.Portfolio.Single(a => a.Id == obj.objectID));
                    context.SaveChanges();
                    break;
                case UDMRequestType.Holding:
                    context.Remove(context.Holding.Single(a => a.Id == obj.objectID));
                    context.SaveChanges();
                    break;
                case UDMRequestType.PortfolioHistory:
                    context.Remove(context.PortfolioHistory.Single(a => a.Id == obj.objectID));
                    context.SaveChanges();
                    break;
            }
            string returnMessage = $"Performed {obj.Operation} on {obj.RequestType}";
            return returnMessage;
        }

        // serializes an object, this line was provided to us by Kevin in the RPC demo code. 
        public static string serialize(dynamic obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings() { Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.All, PreserveReferencesHandling = PreserveReferencesHandling.All });
        }


        
        // The following methods are for converting the entity framework objects (user, portfolio, holding, portfolio history) into an abstract version of the objects. The abstract versions
        // of the objects are the ones that the other components will be using. The entity framework objects can be found in the Models folder. The abstract version of the objects can be 
        // found in the external library called UDMRequestClass. 



        public static User ConvertToUser(UDMUser obj)
        {
            User User = new User();
            User.Email = obj.Email;
            User.FirstName = obj.FirstName;
            User.LastName = obj.LastName;
            User.BrokerageAccount = obj.BrokerageAccount;
            return User;
        }

        public static UDMUser ConvertToUDMUser(User obj)
        {
            UDMUser User = new UDMUser();
            User.Id = obj.Id;
            User.Email = obj.Email;
            User.FirstName = obj.FirstName;
            User.LastName = obj.LastName;
            User.BrokerageAccount = obj.BrokerageAccount;
            return User;
        }

        public static Portfolio ConvertToPortfolio(UDMPortfolio obj)
        {
            Portfolio Portfolio = new Portfolio();
            Portfolio.Active = obj.Active;
            Portfolio.Generated = obj.Generated;
            Portfolio.InitialValue = obj.InitialValue;
            Portfolio.StopValue = obj.StopValue;
            Portfolio.DesiredRisk = (int)obj.DesiredRisk;
            Portfolio.Holding = new List<Holding>();
            foreach (UDMHolding H in obj.Holding)
            {
                Holding TempHolding = new Holding();
                TempHolding.Name = H.Name;
                TempHolding.Abbreviation = H.Abbreviation;
                TempHolding.Description = H.Description;
                TempHolding.Quantity = H.Quantity;
                Portfolio.Holding.Add(TempHolding);
            }
            return Portfolio;
        }

        public static UDMPortfolio ConvertToUDMPortfolio(Portfolio obj)
        {
            UDMPortfolio Portfolio = new UDMPortfolio();
            Portfolio.Id = obj.Id;
            if (!(obj.OwnerId is null)){
                Portfolio.OwnerId = (int)obj.OwnerId;
            }
            Portfolio.Active = obj.Active;
            Portfolio.Generated = obj.Generated;
            Portfolio.InitialValue = obj.InitialValue;
            Portfolio.StopValue = obj.StopValue;
            Portfolio.DesiredRisk = (Risk)obj.DesiredRisk;
            Portfolio.Holding = new List<UDMHolding>();
            foreach (Holding H in obj.Holding)
            {
                UDMHolding TempHolding = new UDMHolding();
                TempHolding.Id = H.Id;
                TempHolding.PortfolioId = H.PortfolioId;
                TempHolding.Name = H.Name;
                TempHolding.Abbreviation = H.Abbreviation;
                TempHolding.Description = H.Description;
                TempHolding.Quantity = H.Quantity;
                Portfolio.Holding.Add(TempHolding);
            }
            return Portfolio;
        }

        public static Holding ConvertToHolding(UDMHolding obj)
        {
            Holding Holding = new Holding();
            Holding.PortfolioId = obj.PortfolioId;
            Holding.Name = obj.Name;
            Holding.Abbreviation = obj.Abbreviation;
            Holding.Description = obj.Description;
            Holding.Quantity = obj.Quantity;
            return Holding;
        }
        public static PortfolioHistory ConvertToPortfolioHistory(UDMPortfolioHistory obj)
        {
            PortfolioHistory PortfolioHistory = new PortfolioHistory();
            PortfolioHistory.PortfolioId = obj.PortfolioId;
            PortfolioHistory.Date = obj.Date;
            PortfolioHistory.Valuation = obj.Valuation;
            PortfolioHistory.Risk = (int)obj.Risk;
            PortfolioHistory.ActionTakenId = obj.ActionTakenId;
            return PortfolioHistory;
        }

        public static List<UDMPortfolioHistory> ConvertToUDMPortfolioHistory(List<PortfolioHistory> obj)
        {
            List<UDMPortfolioHistory> PortfolioHistory = new List<UDMPortfolioHistory>();
            foreach(PortfolioHistory p in obj)
            {
                UDMPortfolioHistory singleHistory = new UDMPortfolioHistory();
                singleHistory.Id = p.Id;
                singleHistory.PortfolioId = p.PortfolioId;
                singleHistory.Date = p.Date;
                singleHistory.Valuation = p.Valuation;
                singleHistory.Risk = (Risk)p.Risk;
                singleHistory.ActionTakenId = p.ActionTakenId;
                PortfolioHistory.Add(singleHistory);
            }
            return PortfolioHistory;
        }
    }
}
