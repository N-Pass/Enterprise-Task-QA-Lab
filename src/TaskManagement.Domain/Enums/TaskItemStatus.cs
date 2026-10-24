namespace TaskManagement.Domain.Enums;

// Named TaskItemStatus to avoid clashing with System.Threading.Tasks.TaskStatus.
public enum TaskItemStatus
{
    ToDo = 0,
    InProgress = 1,
    Done = 2
}
