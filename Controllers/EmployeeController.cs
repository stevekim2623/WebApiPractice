﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApiPractice.DTOs;
using WebApiPractice.Extensions;
using WebApiPractice.Models;

namespace WebApiPractice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeContext _context;

        public EmployeeController(EmployeeContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<Page<Employee>>> GetEmployees(int page, int pageSize)
        {
            if (_context.Employees == null)
            {
                return NotFound();
            }

            return await _context.Employees.ToPagedAsync(page, pageSize);
        }

        [HttpGet("{name}")]
        public async Task<ActionResult<Employee>> GetEmployee(string name)
        {
            if (_context.Employees == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(name);
            if (employee == null)
            {
                return NotFound();
            }

            return employee;
        }

        [HttpPost("text")]
        public async Task<ActionResult> PostEmployee([FromBody] string textarea)
        {
            if (_context.Employees == null)
            {
                return Problem("Entity set 'EmployeeContext.Employees'  is null.");
            }

            return Created("", null);
        }

        [HttpPost("file")]
        public async Task<ActionResult> PostEmployee(IFormFile file)
        {
            if (_context.Employees == null)
            {
                return Problem("Entity set 'EmployeeContext.Employees'  is null.");
            }

            if (!Path.HasExtension(file.FileName))
            {
                ActionResult actionResult = BadRequest(new { message = "This file has no file extension." });
                return actionResult;
            }

            string extension = Path.GetExtension(file.FileName);
            if (extension.ToLower() == ".json")
            {
                Stream stream = file.OpenReadStream();
                var actionResult = await ReadJson(stream);
                return actionResult;
            }
            else if (extension.ToLower() == ".csv")
            {
                Stream stream = file.OpenReadStream();
                ActionResult actionResult = await ReadCSV(stream);
                return actionResult;
            }
            else
            {
                ActionResult actionResult = BadRequest(new { message = "This file has an unsupported file extension." });
                return actionResult;
            }
        }

        private bool EmployeeExists(string name, string joined)
        {
            return (_context.Employees?.Any(e => e.name == name && e.joined == joined)).GetValueOrDefault();
        }

        private IEnumerable<Employee> EmployeeExists(Employee[] employees)
        {
            List<Employee> employeeList = new List<Employee>();

            foreach (var item in employees)
            {
                if ((_context.Employees?.Any(e => e.name == item.name && e.joined == item.joined)).GetValueOrDefault())
                {
                    employeeList.Add(item);
                }
            }

            return employeeList;
        }

        private async Task<ActionResult> ReadJson(Stream stream)
        {
            if (stream.Length == 0)
            {
                ActionResult actionResult = BadRequest(new { message = "JSON file length is 0" });
                return actionResult;
            }

            Employee[]? json;
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                json = JsonSerializer.Deserialize<Employee[]>(stream, jsonOptions);
            }
            catch (JsonException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            if (json == null)
            {
                return BadRequest(new { message = "Failed to deserialize JSON file." });
            }
            else
            {
                if (json.Length <= 0)
                {
                    return BadRequest(new { message = "The number of employees is confirmed as 0." });
                }

                await _context.Employees.AddRangeAsync(json);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    return BadRequest(new { message = ex.Message, ActionResult = (ActionResult)EmployeeExists(json) });
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { message = ex.Message, ActionResult = (ActionResult)EmployeeExists(json) });
                }
            }

            return Created("", null);
        }

        private async Task<ActionResult> ReadCSV(Stream stream)
        {
            if (stream.Length == 0)
            {
                ActionResult actionResult = BadRequest(new { message = "CSV file length is 0" });
                return actionResult;
            }

            StringReader stringReader;
            using (var reader = new StreamReader(stream))
            {
                string text = await reader.ReadToEndAsync();
                stringReader = new StringReader(text);
            }

            List<Employee> employeesList = new List<Employee>();
            string? line;
            while ((line = stringReader.ReadLine()) != null)
            {
                string[] items = line.Split(',');
                if (items.Length != 4)
                {
                    continue;
                }

                Employee emp = new Employee()
                {
                    name = items[0].Trim(),
                    email = items[1].Trim(),
                    tel = items[2].Trim(),
                    joined = items[3].Trim(),
                };
                employeesList.Add(emp);
            }

            Employee[] employees = employeesList.ToArray();
            await _context.Employees.AddRangeAsync(employees);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { message = ex.Message, ActionResult = (ActionResult)EmployeeExists(employees) });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message, ActionResult = (ActionResult)EmployeeExists(employees) });
            }


            return Created("", null);
        }
    }
}
