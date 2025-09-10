using CarInsurance.Api.Controllers;
using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Tests
{
    public class BoundaryCasesTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly CarService _carService;
        private readonly CarsController _controller;

        public BoundaryCasesTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=carins.db")
            .Options;

            _context = new AppDbContext(options);
            _carService = new CarService(_context);
            _controller = new CarsController(_carService);

            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context?.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task IsInsuranceValid_InvalidDateFormat_ReturnsBadRequest()
        {
            var result = await _controller.IsInsuranceValid(1, "invalid-date");

            Assert.IsType<BadRequestObjectResult>(result.Result);
            var badRequest = result.Result as BadRequestObjectResult;
            Assert.Equal("Invalid or impossible date format. Use YYYY-MM-DD.", badRequest!.Value);
        }

        [Fact]
        public async Task IsInsuranceValid_NonExistentCarId_ReturnsNotFound()
        {
            var result = await _controller.IsInsuranceValid(999, "2024-01-01");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task RegisterClaim_InvalidClaimDateFormat_ReturnsBadRequest()
        {
            var dto = new RegisterClaimDto("2024-01-40", "Test claim", 250.0m);

            var result = await _controller.RegisterClaim(1, dto);

            Assert.IsType<BadRequestObjectResult>(result.Result);
            var badRequest = result.Result as BadRequestObjectResult;
            Assert.Equal("Invalid or impossible claim date format. Use YYYY-MM-DD.", badRequest!.Value);
        }

        [Fact]
        public async Task RegisterClaim_NonExistentCarId_ReturnsNotFound()
        {
            var dto = new RegisterClaimDto("2024-01-01", "Test claim", 250.0m);

            var result = await _controller.RegisterClaim(999, dto);

            Assert.IsType<NotFoundObjectResult>(result.Result);
            var notFound = result.Result as NotFoundObjectResult;
            Assert.Equal("Car with ID 999 not found.", notFound!.Value);
        }

        [Fact]
        public async Task GetCarHistory_NonExistentCarId_ReturnsNotFound()
        {
            var result = await _controller.GetCarHistory(999);

            Assert.IsType<NotFoundObjectResult>(result.Result);
            var notFound = result.Result as NotFoundObjectResult;
            Assert.Equal("Car with ID 999 not found.", notFound!.Value);
        }
    }
}