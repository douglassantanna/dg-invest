using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace CoderPad.Assignment
{
    /*
     There are the following assignments to be done: 
     1. Office Manager
     2. Timetable

    The following class solution does not have to be changed at all 
    */
    public class Solution
    {
        public static async Task Main(string[] args)
        {
            DrawConsoleTitle(1);
            Assignment1.Run();
            DrawConsoleTitle(2);
            await Assignment2.ShowTimeTable();
        }

        private static void DrawConsoleTitle(int number)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("########################");
            Console.WriteLine($"##### Assignment {number} #####");
            Console.WriteLine("########################");
            Console.WriteLine();

        }
    }


    /* Assignment 1: Office Manager

    Requirements
    (1) There are two types of WorkLocations. Office and HomeOffice
    (2) In HomeOffice only one Worker can be added. When somebody adds a second, it throws an InvalidOperationException. 
    (3) There are two Offices: 
        * Rapperswil
        * Chur
    (4) In every office there are at least two workers. You are free in choosing the names for the sample data
    (5) Try to add two workers to one HomeOffice instance and catch the exception
    */

    public interface IWorker
    {
        string GetName();
    }
    public class Worker : IWorker
    {
        public Worker(string workerName)
        {
            WorkerName = workerName;
        }
        public string GetName()
            => WorkerName;
        protected string WorkerName { get; }
    }

    public class OfficeLocation : WorkLocation
    {
        public OfficeLocation(string officeName) : base(WorkLocationTypes.Office)
        {
            Name = officeName;
        }

        public override void AddWorker(IWorker worker)
        {
            // hier unbeschränkt viele, aber nicht doppelt
            // here an unlimited number, but not twice the same worker

            // Also... Name suchen und Case-Insesivive vergleichen und Kultur ist auch mal jolo
            // So... look for a name and compare case-insensitive and culture is sometimes jolo

            // Wenn nicht vorhanden, dann einfügen, sond silently ignore, ist ja wie Erfolg
            // If it doesn't exist, then insert it, but silently ignore it, it's like success

            var workers = GetWorkers();
            if (workers.FirstOrDefault(w => w.GetName().Equals(worker.GetName(), StringComparison.InvariantCultureIgnoreCase)) == null)
            {
                ((IList<IWorker>)workers).Add(worker);
            }
        }
    }
    public class HomeOfficeLocation : WorkLocation
    {
        public HomeOfficeLocation(string officeName) : base(WorkLocationTypes.HomeOffice)
        {
            Name = officeName;
        }

        public override void AddWorker(IWorker worker)
        {
            // hier nur einer viele, aber egal, wenn es der Gleiche nochmals ist
            // here just one many, but it doesn't matter if it's the same again

            // Wenn es der Gleiche ist silently ignore, ist ja wie Erfolg
            // If it's the same silently ignore, it's like success

            var workers = GetWorkers();
            if (workers.FirstOrDefault(w => w.GetName().Equals(worker.GetName(), StringComparison.InvariantCultureIgnoreCase)) != null)
            {

            }
            // Wenn es schon einen anderen hat, Throw
            // If it already has another, Throw
            else if (workers.Count() > 0)
            {

                throw new InvalidOperationException("R.GetString(Home office is already occupied.)"); //--ToDo: Localization
            }
            // HomeOffise besetzen, wenn noch nciht besetzt
            // Occupy home office if not yet occupied
            else
            {
                ((IList<IWorker>)workers).Add(worker);
            }
        }
    }

    public abstract class WorkLocation
    {
        public enum WorkLocationTypes { HomeOffice, Office }
        public WorkLocationTypes ArbeitsplatzTpy { get; protected set; }

        public string Name { get; protected set; }

        public WorkLocation(WorkLocationTypes arbeitsplatzTyp)
        {
            // arbeitsplatzTyp = workplace type
            ArbeitsplatzTpy = arbeitsplatzTyp;
        }

        public WorkLocation(string name)
        {
            Name = name;
        }

        private List<IWorker> _workers = new List<IWorker>();
        public IEnumerable<IWorker> GetWorkers() => _workers;
        public abstract void AddWorker(IWorker worker);
    }

    class Assignment1
    {
        public static void Run()
        {
            /* TODO 
             * Create offices and workers and add them to the list _workLocations. 
             * Also create a homeoffice where you add two employees and catch the exception. 
             */

            #region HomeOffice-Test
            List<WorkLocation> _workLocations = new List<WorkLocation>();
            var myHomeOffice = new HomeOfficeLocation("Mein Zuhause");
            myHomeOffice.AddWorker(new Worker("Hans"));
            try
            {
                myHomeOffice.AddWorker(new Worker("Peter"));
            }
            catch (Exception ex)
            {
                var exName = ex.GetType().Name; //--Fürs Debugging
                //throw; //--Damit der Test hiuer durchläuft, sonst sollte man schon drauf aufmerksam machen
                // So that the test runs through here, otherwise you should already draw attention to it
            }
            finally { } //__ Nothing to do
            #endregion
            #region Office-Test
            var churOffice = new OfficeLocation("Chur");
            churOffice.AddWorker(new Worker("Rolf"));
            churOffice.AddWorker(new Worker("rolf"));
            churOffice.AddWorker(new Worker("Sandra"));

            var rapperswilOffice = new OfficeLocation("Rapperswil");
            rapperswilOffice.AddWorker(new Worker("Stefan"));
            rapperswilOffice.AddWorker(new Worker("Florian"));

            IList<WorkLocation> _OfficeLocations = new List<WorkLocation>() {
                churOffice,
                rapperswilOffice,
            };

            Console.WriteLine();
            Console.WriteLine("# Locations (without Homeoffice) with its workers");
            /* 
              TODO Print a list of all Locations and the persons working there. Don't list persons working in Homeoffice. Example:
              # Chur 
              * Hans
              * Rolf               
              # Rapperswil
              * Stefan
              * Peter          

             */
            foreach (var office in _OfficeLocations)
            {
                Console.WriteLine(office.Name);
                foreach (var worker in office.GetWorkers())
                {
                    Console.WriteLine($"\t{worker.GetName()}");
                }
            }
            #endregion
            Console.WriteLine();
            Console.WriteLine("# All Employees ordered by Name");
            /* 
              TODO Print a list of all employees ordered by name Example: 

              * Hans
              * Peter
              * Rolf
              * Sandra
              * Stefan

             */
            var allWorkersInAllOffices = new List<string>(); //
            foreach (var office in _OfficeLocations)
                foreach (var worker in office.GetWorkers())
                    allWorkersInAllOffices.Add(worker.GetName());
            foreach (var name in allWorkersInAllOffices.OrderBy(n => n))
                Console.WriteLine(name);
        }
    }

    /*
    Assignment 2: Timetable

    Requirements: 
    (1) The application shall show the stationboard of the train station in Chur. 
    (2) The board shall show every train departure in the next hour. 
    (3) The current time (in the timezone "Europe/Zurich") shall be written above
    (4) The output shall be formatted like this:

    Current Time: 07:06
    07:08 Basel SBB              9
    07:08 Arosa                  2
    07:08 Rhäzüns               11 BDZ DOE
    07:11 Luzern                 5
    07:16 Bern                   8
    07:21 Scuol-Tarasp          12
    07:31 Sargans                5
    07:48 Thusis                14 BDZ DOE
    07:53 Schiers               12
    07:56 Disentis/Mustér       11
    07:58 St. Moritz            10 DOE
    08:01 Sargans                4
    08:08 Zürich HB              9
    08:08 Arosa                  2
    08:08 Rhäzüns               11 BDZ DOE

    Each line consists of the following values:
    * Time in 24 hour format
    * Destination of the train.
    * Platform
    * If the train stops in Bonaduz: "BDZ"
    * If the train stops in Domat/Ems: "DOE"
    */

    public class Assignment2
    {
        /*
        TODO Get the data using a HTTP GET Request on http://transport.opendata.ch/v1/stationboard?station=Chur&limit=20.
        */
        private static async Task<string> RequestData()
        {
            using (var httpClient = new HttpClient())
            {
                var request = await httpClient.GetAsync("http://transport.opendata.ch/v1/stationboard?station=Chur&limit=20");
                if (request.IsSuccessStatusCode)
                {
                    return await request.Content.ReadAsStringAsync();
                }
                else
                {
                    //--ToDo: Wenn es nicht klapp, dann ist der ganze HttpClient futsch.
                    //--ToDo: If it doesn't work, then the whole HttpClient is gone.
                    //--also müssen wir einen äussernen recovery loop haben, was diesen Test übersteigt
                    //--so we must have an outer recovery loop that exceeds this test
                    Console.WriteLine($"Didn't work. Reason: {request.ReasonPhrase}");
                    return null;
                }
            }
            //return await Task.FromResult(string.Empty);
        }

        /* 
         * Does not need to be changed
         */
        private static Root ConvertJson(string content)
        {
            if (content.CompareTo(string.Empty) == 0)
            {
                Console.WriteLine("No Json to convert");
                return new Root();
            }
            var result = JsonSerializer.Deserialize<Root>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            if (result == null)
            {
                throw new Exception("Json could not be parsed");
            }
            return result;
        }

        /* 
        TODO Print the stationboard. The columns and the format should be as in the requirements above
        */
        private static void PrintStationboard(Root root)
        {
            Console.WriteLine($"Current Time: {DateTime.Now.ToString("HH:mm")}");
            foreach (var sb in root.Stationboard)
            {
                foreach (var pl in sb.PassList
                    //.Where(pass => DateTime
                    //.TryParse(pass.Departure, out DateTime dateTime) && dateTime >= DateTime.Now)
                    )
                {
                    Console.WriteLine($"{(DateTime.TryParse(pl.Departure, out DateTime dt) ? dt.ToString("HH:mm") : string.Empty)} {pl.Station.Name} {pl.Platform}");

                }
            }

            Console.WriteLine(root.Stationboard[0].To);
        }

        public static async Task ShowTimeTable()
        {
            string response = await RequestData();
            var root = ConvertJson(response);
            PrintStationboard(root);
        }

    }

#pragma warning disable CS8618
    public class Root
    {
        public List<Stationboard> Stationboard { get; set; }
    }

    public class Station
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public object Score { get; set; }
        public object Distance { get; set; }
    }

    public class Stationboard
    {
        public string To { get; set; }
        public List<Pass> PassList { get; set; }
    }

    public class Pass
    {
        public Station Station { get; set; }
        public string Departure { get; set; }
        public string Platform { get; set; }
    }
}
#pragma warning restore CS8618
