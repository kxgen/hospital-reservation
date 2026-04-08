using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Dtos.Requests;
using Backend.Dtos.Responses;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorsController : ControllerBase
    {
        private readonly DoctorRepository _doctorRepository;
        private readonly string _connectionString;

        public DoctorsController(DoctorRepository doctorRepository, IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") ?? "";
            _doctorRepository = doctorRepository;
        }

        [HttpGet]
        public IActionResult GetDoctors([FromQuery] string? search = null, [FromQuery] string? specialties = null, [FromQuery] string? gender = null)
        {
            var doctors = _doctorRepository.GetAllDoctors(search, specialties, gender);
            var appointmentRepo = new AppointmentRepository(_connectionString);
            
            var result = new List<DoctorResponse>();
            foreach (var d in doctors)
            {
                
                var slots = appointmentRepo.GetAvailableSlotsByDoctorId(d.DoctorId);
                result.Add(MapToResponse(d, slots));
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public IActionResult GetDoctorById(int id)
        {
            var doctor = _doctorRepository.GetDoctorById(id);
            if (doctor == null) return NotFound();

            var appointmentRepo = new AppointmentRepository(_connectionString);
            var slots = appointmentRepo.GetAvailableSlotsByDoctorId(id);

            return Ok(MapToResponse(doctor, slots));
        }

        [HttpGet("{id}/slots")]
        public IActionResult GetSlots(int id)
        {
            var appointmentRepo = new AppointmentRepository(_connectionString);
            var slots = appointmentRepo.GetAvailableSlotsByDoctorId(id);
            return Ok(slots);
        }

        [HttpGet("{id}/full-schedule")]
        public IActionResult GetFullSchedule(int id)
        {
            var appointmentRepo = new AppointmentRepository(_connectionString);
            var slots = appointmentRepo.GetAllSlotsByDoctorId(id);
            return Ok(slots);
        }

        private DoctorResponse MapToResponse(Doctor d, List<TimeSlotResponse>? slots = null)
        {
            return new DoctorResponse
            {
                Id = d.DoctorId,
                FirstName = d.FirstName,
                LastName = d.LastName,
                SpecialtyId = d.SpecialtyId,
                SpecialtyName = d.SpecialtyName,
                Gender = d.Gender ?? "Not Specified",
                Bio = d.Bio ?? "",
                PhotoUrl = d.PhotoUrl ?? "",
                Timeslots = slots ?? new List<TimeSlotResponse>()
            };
        }

        [HttpGet("specialties")]
        public IActionResult GetSpecialties()
        {
            var specialties = _doctorRepository.GetAllSpecialties();
            var result = specialties.Select(s => new SpecialtyResponse { Id = s.SpecialtyId, Name = s.SpecialtyName });
            return Ok(result);
        }

        [HttpGet("specialties_full")]
        public IActionResult GetSpecialtiesFull()
        {
             
             var list = _doctorRepository.GetAllSpecialties();
             var result = list.Select(s => new SpecialtyResponse { Id = s.SpecialtyId, Name = s.SpecialtyName });
             return Ok(result);
        }


    }
}
