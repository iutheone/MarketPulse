//public class Program
//{
//    public static void Main()
//    {
//        var system = new EmployeeManagementSystem();

//        var engineering = system.CreateDepartment("Engineering");
//        var hr = system.CreateDepartment("HR");


//        var manager =
//            system.AddEmployee(
//                EmployeeType.FullTime,
//                "Rahul",
//                "rahul@company.com",
//                1200000);

//        var developer =
//            system.AddEmployee(
//                EmployeeType.FullTime,
//                "Ayu",
//                "ayu@company.com",
//                900000);

//        var intern =
//            system.AddEmployee(
//                EmployeeType.Intern,
//                "Rohit",
//                "rohit@company.com",
//                20000);


//        system.AssignEmployeeToDepartment(
//           manager.Id,
//           engineering.Id);

//        system.AssignEmployeeToDepartment(
//            developer.Id,
//            engineering.Id);

//        system.AssignEmployeeToDepartment(
//            intern.Id,
//            engineering.Id);

//        system.AssignMananger(
//            developer.Id,
//            manager.Id);

//        system.AssignMananger(
//            intern.Id,
//            manager.Id);


//        decimal salary =
//          system.GetSalary(developer.Id);

//        Console.WriteLine(
//            $"Monthly salary: {salary}");


//        system.AddLeaveObserver(
//            new EmailNotificationObserver());

//        system.AddLeaveObserver(
//            new SmsLeaveObserver());

//        system.AddLeaveObserver(
//            new PushNotificationObserver());


//        var leave =
//          system.ApplyLeave(
//              developer.Id,
//              LeaveType.Casual,
//              DateTime.Today,
//              DateTime.Today.AddDays(4),
//              "Family function");



//        Console.WriteLine(
//            $"Leave days: {leave.NumberOfDays}");

//        Console.WriteLine(
//            $"Leave status: {leave.Status}");

//        system.Approve(leave.Id);


//        Console.WriteLine(
//            $"Final status: {leave.Status}");
//    }
//}



//public enum EmployeeType
//{
//    FullTime,
//    Contract,
//    Intern
//}


//public enum LeaveType
//{
//    Casual,
//    Sick,
//    Earned
//}
//public enum LeaveStatus
//{
//    Pending,
//    Approved,
//    Rejected
//}

//public abstract class Employee
//{
//    public Guid Id { get; }
//    public string Name { get; }
//    public string Email { get; }
//    public EmployeeType Type { get; }

//    public Department? Department { get; set; }
//    public Employee? Manager { get; set; }

//    protected Employee(
//        string name,
//        string email,
//        EmployeeType type)
//    {
//        Id = Guid.NewGuid();
//        Name = name;
//        Email = email;
//        Type = type;
//    }
//}


//public class FullTimeEmployee : Employee
//{
//    public decimal AnnualSalary { get; }
//    public FullTimeEmployee(string name, string email, decimal annualSalary) : base(name, email, EmployeeType.FullTime)
//    {
//        AnnualSalary = annualSalary;
//    }
//}


//public class ContractEmployee : Employee
//{
//    public decimal MonthlySalary { get; }

//    public ContractEmployee(
//        string name,
//        string email,
//        decimal monthlySalary)
//        : base(name, email, EmployeeType.Contract)
//    {
//        MonthlySalary = monthlySalary;
//    }
//}

//public class Intern : Employee
//{
//    public decimal Stipend { get; }

//    public Intern(
//        string name,
//        string email,
//        decimal stipend)
//        : base(name, email, EmployeeType.Intern)
//    {
//        Stipend = stipend;
//    }
//}
//public class Department
//{
//    public Guid Id { get; } = Guid.NewGuid();
//    public string Name { get; }
//    public readonly List<Employee> _employees = new();
//    public IReadOnlyList<Employee> Employees => _employees;
//    public Department(string name)
//    {
//        Name = name;
//    }

//    public void AddEmployee(Employee employee)
//    {
//        if (!_employees.Contains(employee))
//        {
//            _employees.Add(employee);
//            employee.Department = this;
//        }
//    }

//    public void RemoveEmployee(Employee employee)
//    {
//        _employees.Remove(employee);
//        if(employee.Department == this)
//        {
//            employee.Department = null;
//        }
//    }    
//}

////Employee factory
//public class EmployeeFactory
//{
//    public Employee CreateEmployee(EmployeeType type, string name,string email, decimal salary)
//    {
//        return type switch
//        {
//            EmployeeType.FullTime => new FullTimeEmployee(name, email, salary),
//            EmployeeType.Contract => new ContractEmployee(name, email, salary),
//            EmployeeType.Intern => new Intern(name, email, salary),
//            _ => throw new ArgumentException("invalid arguments")
//        };
//    }
//}


////Salary strategy
//public interface ISalaryCalculator
//{
//    public decimal CalculateSalary(Employee employee);
//}

//public class FullTimeSalaryCalculator : ISalaryCalculator
//{
//    public decimal CalculateSalary(Employee employee)
//    {
//        var emp = (FullTimeEmployee)employee;
//        return emp.AnnualSalary/12;
//    }
//}

//public class ContractSalaryCalculator: ISalaryCalculator
//{
//    public decimal CalculateSalary(Employee employee)
//    {
//        var emp = (ContractEmployee)employee;
//        return emp.MonthlySalary;
//    }
//}

//public class InternSalaryCalculator : ISalaryCalculator
//{
//    public decimal CalculateSalary(Employee employee)
//    {
//        var emp = (Intern)employee;

//        return emp.Stipend;
//    }
//}


//public class SalaryCalculatorFactory
//{
//    public ISalaryCalculator GetCalculator(EmployeeType type)
//    {
//        return type switch
//        {
//            EmployeeType.FullTime =>
//                new FullTimeSalaryCalculator(),

//            EmployeeType.Contract =>
//                new ContractSalaryCalculator(),

//            EmployeeType.Intern =>
//                new InternSalaryCalculator(),

//            _ => throw new ArgumentException("Invalid employee type")
//        };
//    }
//}



//// ================= LEAVE REQUEST =================
//public class LeaveRequest
//{
//    public Guid Id { get; } = Guid.NewGuid();
//    public Employee Employee { get; }
//    public LeaveType LeaveType { get; }
//    public DateTime StartTime { get; }
//    public DateTime EndTime { get; }
//    public string Reason { get; }
//    public LeaveStatus Status { get; private set; }

//    public int NumberOfDays => (EndTime - StartTime).Days + 1;

//    public LeaveRequest(Employee employee, LeaveType type, DateTime startTime, DateTime endTime, string reason)
//    {
//        Employee = employee;
//        LeaveType = type;
//        StartTime = startTime;
//        EndTime = endTime;
//        Reason = reason;
//        Status = LeaveStatus.Pending;
//    }

//    public void Approve()
//    {
//        if(Status != LeaveStatus.Pending)
//        {
//            throw new InvalidOperationException("leave is also processed");
//        }
//        Status = LeaveStatus.Approved;
//    }

//    public void Reject()
//    {
//        if(Status == LeaveStatus.Rejected)
//        {
//            throw new InvalidOperationException("Leave is also rejected");

//        }

//        Status = LeaveStatus.Rejected;
//    }
//}

////Leave Policy
//public interface ILeavePolicy
//{
//    bool CanApply(Employee employee, LeaveRequest leaveRequest);
//}

//public class FullTimeLeavePolicy: ILeavePolicy
//{
//    public bool CanApply(Employee employee, LeaveRequest request)
//    {
//        return request.NumberOfDays <= 20;
//    }
//}



//public class ContractLeavePolicy : ILeavePolicy
//{
//    public bool CanApply(Employee employee, LeaveRequest request)
//    {
//        // Example: maximum 10 days
//        return request.NumberOfDays <= 10;
//    }
//}


//public class InternLeavePolicy : ILeavePolicy
//{
//    public bool CanApply(Employee employee, LeaveRequest request)
//    {
//        // Example: maximum 5 days
//        return request.NumberOfDays <= 5;
//    }
//}


//// ================= LEAVE POLICY FACTORY =================

//public class LeavePolicyFactory
//{
//    public ILeavePolicy GetPolicy(EmployeeType type)
//    {
//        return type switch
//        {
//            EmployeeType.FullTime =>
//                new FullTimeLeavePolicy(),

//            EmployeeType.Contract =>
//                new ContractLeavePolicy(),

//            EmployeeType.Intern =>
//                new InternLeavePolicy(),

//            _ => throw new ArgumentException("Invalid employee type")
//        };
//    }
//}

//public abstract class LeaveApprovalHandler
//{
//    public LeaveApprovalHandler? Next;
//    public void SetNext(LeaveApprovalHandler next)
//    {
//        Next = next;
//    }

//    public abstract bool Approve(LeaveRequest request);
//}


////Manager Approval
//public class ManagerApprovalHandler: LeaveApprovalHandler
//{
//    public override bool Approve(LeaveRequest request)
//    {
//        Console.WriteLine(
//            $"Manager approved leave for {request.Employee.Name}");
//        return Next?.Approve(request) ?? true;
//    }
//}

//// Department Head approval
//public class DepartmentHeadApprovalHandler : LeaveApprovalHandler
//{
//    public override bool Approve(LeaveRequest request)
//    {
//        Console.WriteLine(
//            $"Department Head approved leave for {request.Employee.Name}");

//        return Next?.Approve(request) ?? true;
//    }
//}


//// HR approval
//public class HrApprovalHandler : LeaveApprovalHandler
//{
//    public override bool Approve(LeaveRequest request)
//    {
//        Console.WriteLine(
//            $"HR approved leave for {request.Employee.Name}");

//        return Next?.Approve(request) ?? true;
//    }
//}

//// ================= APPROVAL CHAIN FACTORY =================
//public class LeaveApprovalChainFactory
//{
//    public LeaveApprovalHandler CreateChain(int numberOfDays)
//    {
//        var manager = new ManagerApprovalHandler();
//        if(numberOfDays <= 2)
//        {
//            return manager;
//        }

//        var deparmentHead = new DepartmentHeadApprovalHandler();
//        manager.SetNext(deparmentHead);

//        //3-5 days
//        if (numberOfDays <= 5)
//        {
//            return manager;
//        }
//        var hr = new HrApprovalHandler();

//        deparmentHead.SetNext(hr);

//        // > 5 days
//        return manager;
//    }
//}



//// ================= NOTIFICATION =================
//public interface ILeaveObserver
//{
//    public void Notify(LeaveRequest request);
//}

//public class EmailNotificationObserver : ILeaveObserver
//{
//    public void Notify(LeaveRequest request)
//    {
//        Console.WriteLine($"Email sent to {request.Employee.Email}");
//    }
//}

//public class SmsLeaveObserver : ILeaveObserver
//{
//    public void Notify(LeaveRequest request)
//    {
//        Console.WriteLine(
//            $"SMS sent to {request.Employee.Name}");
//    }
//}


//public class PushNotificationObserver : ILeaveObserver
//{
//    public void Notify(LeaveRequest request)
//    {
//        Console.WriteLine(
//            $"Push notification sent to {request.Employee.Name}");
//    }
//}

//// ================= EMPLOYEE MANAGEMENT SYSTEM =================

//public class EmployeeManagementSystem
//{
//    private readonly List<Employee> _employees = new();
//    private readonly List<Department> _departments = new();


//    private readonly List<LeaveRequest> _leaveRequests = new();
//    private readonly EmployeeFactory _employeeFactory = new();
//    private readonly SalaryCalculatorFactory _salaryFactory = new();

//    private readonly LeavePolicyFactory _leavePolicyFactory = new();

//    private readonly LeaveApprovalChainFactory _approvalFactory = new();

//    private readonly List<ILeaveObserver> _leaveObservers = new();


//    // ================= EMPLOYEE =================
//    public Employee AddEmployee(
//        EmployeeType type,
//        string name,
//        string email,
//        decimal salary)
//    {
//        var employee = _employeeFactory.CreateEmployee(
//            type,
//            name,
//            email,
//            salary);

//        _employees.Add(employee);

//        return employee;
//    }

//    public Employee? GetEmployee(Guid id)
//    {
//        return _employees.FirstOrDefault(x => x.Id == id);
//    }

//    public List<Employee> GetAllEmployees()
//    {
//        return _employees.ToList();
//    }


//    public void RemoveEmployee(Guid id)
//    {
//        var employee = GetEmployee(id);

//        if (employee != null)
//        {
//            _employees.Remove(employee);
//        }
//    }


//    // ================= DEPARTMENT =================

//    public Department CreateDepartment(string name)
//    {
//        var department = new Department(name);
//        _departments.Add(department);
//        return department;
//    }

//    public void AssignEmployeeToDepartment(Guid empId, Guid departmentId)
//    {
//        var employee = _employees.FirstOrDefault(i => i.Id == empId);
//        var department = _departments.FirstOrDefault(i => i.Id == departmentId);
//        if(employee == null)
//        {
//            throw new Exception("Employee not found");
//        }
//        if(department  == null)
//        {
//            throw new Exception("Department not found");
//        }

//        department.AddEmployee(employee);
//    }

//    public void AssignMananger(Guid empId, Guid ManagerID)
//    {
//        var employee = GetEmployee(empId);
//        var manager = GetEmployee(ManagerID) ;
//        if(employee == null || manager == null)
//        {
//            throw new Exception("Employee not found.");
//        }

//        employee.Manager = manager;
//    }


//    // ================= SALARY =================
//    public decimal GetSalary(Guid empId)
//    {
//        var employee = GetEmployee(empId);
//        var calculator = _salaryFactory.GetCalculator(employee.Type);
//        return calculator.CalculateSalary(employee);
//    }

//    // ================ Leave ===================
//    public LeaveRequest ApplyLeave(Guid empId, LeaveType leaveType, DateTime startdate,
//        DateTime endDate, string reason)
//    {
//        var employee = GetEmployee(empId);
//        var leave = new LeaveRequest(employee, leaveType, startdate, endDate, reason);
//        var leavePolicy = _leavePolicyFactory.GetPolicy(employee.Type);
//        if (!leavePolicy.CanApply(employee, leave))
//        {
//            throw new Exception("User is not eligible to apply leave");
//        }

//        _leaveRequests.Add(leave);
//        return leave;
//    }

//    public void Approve(Guid leaveId)
//    {
//        var leaveReq = _leaveRequests.FirstOrDefault(t => t.Id == leaveId);

//        var chain = _approvalFactory.CreateChain(leaveReq.NumberOfDays);
//        bool approved = chain.Approve(leaveReq);
//        if (approved)
//        {
//            leaveReq.Approve();
//        }

//    }

//    // =============== Observers ================
//    public void AddLeaveObserver(ILeaveObserver observer)
//    {
//        _leaveObservers.Add(observer);
//    }

//    public void NotifyObserver(LeaveRequest request)
//    {
//        _leaveObservers.ForEach(t => t.Notify(request));
//    }


//    // =============Search ======================

//    public List<Employee> SearchByName(string name)
//    {
//        var employee = _employees.Where(t => t.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
//        return employee.ToList();
//    }

//    public List<Employee> SearchByDepartment(
//       string departmentName)
//    {
//        return _employees
//            .Where(x =>
//                x.Department?.Name.Equals(
//                    departmentName,
//                    StringComparison.OrdinalIgnoreCase) == true)
//            .ToList();
//    }

//}


var employees = new List<Employee>
{
    new() { Id = 1, Name = "Amit", DepartmentId = 1, Salary = 90000,
            Skills = new() { "C#", "SQL", "React" } },

    new() { Id = 2, Name = "Rahul", DepartmentId = 1, Salary = 75000,
            Skills = new() { "C#", "SQL" } },

    new() { Id = 3, Name = "Priya", DepartmentId = 2, Salary = 120000,
            Skills = new() { "C#", "Azure", "Docker" } },

    new() { Id = 4, Name = "Neha", DepartmentId = 2, Salary = 95000,
            Skills = new() { "Java", "SQL" } },

    new() { Id = 5, Name = "Vikas", DepartmentId = 3, Salary = 80000,
            Skills = new() { "C#", "Docker" } },

    new() { Id = 6, Name = "Ravi", DepartmentId = 3, Salary = 80000,
            Skills = new() { "C#", "Docker" } },

    new() { Id = 7, Name = "Sneha", DepartmentId = 3, Salary = 70000,
            Skills = new() { "SQL" } }
};


public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int DepartmentId { get; set; }
    public decimal Salary { get; set; }
    public List<string> Skills { get; set; }
}

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; }
}


var departments = new List<Department>
{
    new() { Id = 1, Name = "Engineering" },
    new() { Id = 2, Name = "Cloud" },
    new() { Id = 3, Name = "QA" },
    new() { Id = 4, Name = "HR" }
};