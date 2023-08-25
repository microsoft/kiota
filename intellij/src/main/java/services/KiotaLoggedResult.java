package services;

import java.util.List;

public class KiotaLoggedResult {
    private List<KiotaLogEntry> logs;
    public List<KiotaLogEntry> getLogs() {
        return logs;
    }
    public void setLogs(List<KiotaLogEntry> logs) {
        this.logs = logs;
    }
}