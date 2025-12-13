using CaseFlow.Domain.Enums;

namespace CaseFlow.Domain.Models;

public class Employee
{
    public int Id { get; set; }

    public string ApplicationUserId { get; set; } = default!;
    
    public string Name { get; set; } = string.Empty;
    
    public UserRole Role { get; set; }
    
    public int? DepartmentId { get; set; }
    
    public Department Department { get; set; }
    
}