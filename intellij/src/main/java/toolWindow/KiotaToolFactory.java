package toolWindow;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.wm.ToolWindow;
import com.intellij.openapi.wm.ToolWindowFactory;
import com.intellij.ui.content.Content;
import com.intellij.ui.content.ContentFactory;
import org.jetbrains.annotations.NotNull;
import java.io.IOException;
public class KiotaToolFactory implements ToolWindowFactory {
    @Override
    public void createToolWindowContent(@NotNull Project project, @NotNull ToolWindow toolWindow) {
        MyToolWindow myToolWindow = new MyToolWindow(toolWindow);
        ContentFactory contentFactory = ContentFactory.getInstance();
        Content content = null;
        try {
            content = contentFactory.createContent(myToolWindow.Addpanel(), null, false);
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
        toolWindow.getContentManager().addContent(content);
    }
    @Override
    public boolean shouldBeAvailable(@NotNull Project project) {
        return true;
    }

}
