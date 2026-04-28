using Xunit;
using HRM.BLL.Services;
using HRM.Common.DTOs;
using System;

namespace HRM.Tests
{
    public class HieuSuatServiceTests
    {
        [Theory]
        [InlineData(80.0, -1.0, 80.0)] // Use -1.0 as null substitute for simplicity if needed, but let's try double
        [InlineData(-1.0, 90.0, 90.0)]
        [InlineData(100.0, 100.0, 100.0)]
        [InlineData(0.0, 0.0, 0.0)]
        [InlineData(80.0, 60.0, 74.0)] 
        public void TinhDiemHieuSuatCuoiCung_CalculatesCorrectly(double kpi, double deadline, double expected)
        {
            decimal? dKpi = kpi < 0 ? null : (decimal)kpi;
            decimal? dDeadline = deadline < 0 ? null : (decimal)deadline;
            decimal? dExpected = expected < 0 ? null : (decimal)expected;

            var result = HieuSuatService.TinhDiemHieuSuatCuoiCung(dKpi, dDeadline);
            Assert.Equal(dExpected, result);
        }

        [Theory]
        [InlineData(105.0, 100.0, "Hoàn thành vượt mức")]
        [InlineData(90.0, 90.0, "Hoàn thành")]
        [InlineData(75.0, 75.0, "Hoàn thành một phần")]
        [InlineData(50.0, 50.0, "Chưa hoàn thành")]
        [InlineData(100.0, -1.0, "Hoàn thành vượt mức")]
        public void DanhGiaTrangThaiHoanThanh_ReturnsCorrectStatus(double deadline, double tongDiem, string expected)
        {
            decimal? dDeadline = deadline < 0 ? null : (decimal)deadline;
            decimal? dTongDiem = tongDiem < 0 ? null : (decimal)tongDiem;

            var result = HieuSuatService.DanhGiaTrangThaiHoanThanh(dDeadline, dTongDiem);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(95.0, 90.0, 0.20)]   
        [InlineData(90.0, 90.0, 0.15)]   
        [InlineData(85.0, 80.0, 0.10)]   
        [InlineData(75.0, 75.0, 0.05)]   
        [InlineData(65.0, 70.0, 0.00)]   
        [InlineData(55.0, 70.0, -0.10)]  
        [InlineData(80.0, 60.0, 0.05)]   
        public void TinhHeSoLuongHieuSuat_CalculatesCorrectly(double tongDiem, double deadline, double expected)
        {
            decimal? dTongDiem = (decimal)tongDiem;
            decimal? dDeadline = (decimal)deadline;
            decimal dExpected = (decimal)expected;

            var result = HieuSuatService.TinhHeSoLuongHieuSuat(dTongDiem, dDeadline);
            Assert.Equal(dExpected, result);
        }

        [Fact]
        public void DemSoNgayLamViec_CountsCorrectly()
        {
            var start = new DateTime(2024, 4, 1);
            var end = new DateTime(2024, 4, 5);
            Assert.Equal(5, HieuSuatService.DemSoNgayLamViec(start, end));

            var start2 = new DateTime(2024, 4, 5);
            var end2 = new DateTime(2024, 4, 8);
            Assert.Equal(2, HieuSuatService.DemSoNgayLamViec(start2, end2));

            var start3 = new DateTime(2024, 4, 6); 
            var end3 = new DateTime(2024, 4, 7); 
            Assert.Equal(0, HieuSuatService.DemSoNgayLamViec(start3, end3));
        }

        [Theory]
        [InlineData(10000000.0, 0.10, 160.0, 11000000.0)] 
        [InlineData(10000000.0, 0.0, 160.0, 10000000.0)]    
        [InlineData(10000000.0, 0.20, 192.0, 14400000.0)] 
        [InlineData(10000000.0, -0.10, 128.0, 7200000.0)] 
        [InlineData(10000000.0, 0.0, 200.0, 12000000.0)]    
        [InlineData(10000000.0, 0.0, 80.0, 8000000.0)]      
        public void TinhLuongDuKien_CalculatesCorrectly(double baseSalary, double bonusRate, double hours, double expected)
        {
            decimal dBaseSalary = (decimal)baseSalary;
            decimal dBonusRate = (decimal)bonusRate;
            decimal? dHours = (decimal)hours;
            decimal dExpected = (decimal)expected;

            var result = HieuSuatService.TinhLuongDuKien(dBaseSalary, dBonusRate, dHours);
            Assert.Equal(dExpected, result);
        }
    }
}
