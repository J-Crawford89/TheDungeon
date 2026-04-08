public interface IGameLog
{
	void AppendGameLog(string line);
	void AppendLog(LogEntry entry);
}
