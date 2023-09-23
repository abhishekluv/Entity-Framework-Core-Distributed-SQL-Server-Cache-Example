# Entity Framework Core Distributed SQL Server Cache

Caching can improve the performance and scalability of an app, especially when the app is hosted by a cloud service or a server farm. 

In ASP.NET Core distributed caching can be implemented with the help of IDistributedCache interface - we can start with MemoryCache in the development and in production we can switch to SQL Server provider which implements IDistributedCache interface.

First we need to install the dotnet tool which helps us to setup the caching infrastructure.

We can do this by running this command:

`dotnet tool install --global dotnet-sql-cache.`

We can use this tool to create the required Cache table in SQL Server Database:

`dotnet sql-cache create "Data Source=LAPTOP-S73IQID2\SQLEXPRESS;Initial Catalog=EFCoreDistributedSQLServerCache;Integrated Security=True;TrustServerCertificate=true;" dbo DatabaseCache`

![](https://i.imgur.com/7ACzcYx.jpeg)

![](https://i.imgur.com/Dk8P7D0.jpeg)


The `Microsoft.Extensions.Caching.SqlServer` package is required in order to setup SQL Server Distributed Cache. 

We need to run this command `Install-Package Microsoft.Extensions.Caching.SqlServer` in Nuget package manager console.

The Distributed SQL Server Cache implementation `AddDistributedSqlServerCache` allows the distributed cache to use a SQL Server database as its backing store. 

![](https://i.imgur.com/kYQ2Shx.jpeg)


```csharp
using EFCoreDistributedSQLServerCache.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace EFCoreDistributedSQLServerCache.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly MyApplicationContext _context;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<EmployeesController> _logger;
        private const string EMPLOYEES_CACHE = "Employees";

        public EmployeesController(MyApplicationContext context, IDistributedCache distributedCache, ILogger<EmployeesController> logger)
        {
            _context = context;
            _distributedCache = distributedCache;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cachedEmpsJson = _distributedCache.GetString(EMPLOYEES_CACHE);

            if (cachedEmpsJson == null)
            {
                _logger.LogInformation("Cached Missed");

                var employees = _context.Employees
                                        .ToList();

                var empsJson = JsonSerializer.Serialize(employees);
                _distributedCache.SetString(EMPLOYEES_CACHE, empsJson);

                return View(employees);
            }

            _logger.LogInformation("Reading from Cache");
            var cachedEmployees = JsonSerializer.Deserialize<List<Employee>>(cachedEmpsJson);
            return View(cachedEmployees);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Employee employeeModel)
        {
            if (ModelState.IsValid)
            {
                _context.Employees.Add(employeeModel);
                _context.SaveChanges();

                //evicting the cache
                _distributedCache.Remove(EMPLOYEES_CACHE);

                _logger.LogInformation($"{EMPLOYEES_CACHE} cache evicted");

                return RedirectToAction("Index");
            }

            return View(employeeModel);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var employee = _context.Employees.Find(id);
            return View(employee);
        }

        [HttpPost]
        public IActionResult Edit(Employee employeeModel)
        {
            if (ModelState.IsValid)
            {
                _context.Employees.Update(employeeModel);
                _context.SaveChanges();

                //evicting the cache
                _distributedCache.Remove(EMPLOYEES_CACHE);

                _logger.LogInformation($"{EMPLOYEES_CACHE} cache evicted");

                return RedirectToAction("Index");
            }

            return View(employeeModel);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var employee = _context.Employees.Find(id);
            return View(employee);
        }

        [HttpPost]
        [ActionName("Delete")]
        public IActionResult DeletePost(int id)
        {
            var employee = _context.Employees.Find(id);

            _context.Employees.Remove(employee);
            _context.SaveChanges();

            //evicting the cache
            _distributedCache.Remove(EMPLOYEES_CACHE);
            _logger.LogInformation($"{EMPLOYEES_CACHE} cache evicted");

            return RedirectToAction("Index");
        }
    }
}
```  