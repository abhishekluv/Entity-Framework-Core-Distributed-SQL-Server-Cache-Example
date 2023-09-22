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