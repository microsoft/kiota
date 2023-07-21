package toolWindow;

import com.intellij.openapi.project.Project; // change this later
import com.intellij.openapi.ui.LabeledComponent;
import com.intellij.openapi.ui.TextFieldWithBrowseButton;
import com.intellij.openapi.wm.ToolWindow;
import com.intellij.ui.components.JBPanel;
import com.intellij.ui.components.panels.VerticalLayout;
import services.MyKiotaProjectService;

import javax.swing.*;
import java.awt.*;

public class MyToolWindow {
   // private final MyKiotaProjectService service;
    private final JBPanel<JBPanel<?>> ParentPanel;

    public MyToolWindow(ToolWindow toolWindow) {
        Project project = toolWindow.getProject();
       // service = project.getService(MyKiotaProjectService.class);
        ParentPanel = new JBPanel<>();
    }

    public JComponent Addpanel() {
        ParentPanel.setLayout(new BorderLayout());
        ParentPanel.add(getInput(), BorderLayout.CENTER);
        return ParentPanel;
    }

    public JComponent getInput() {
        JBPanel<JBPanel<?>> mainPanel = new JBPanel<>();
        mainPanel.setLayout(new VerticalLayout(10));

        LabeledComponent<TextFieldWithBrowseButton> labeledField1 = LabeledComponent.create(new TextFieldWithBrowseButton(), "Enter the YAML file Path");
        LabeledComponent<TextFieldWithBrowseButton> labeledField2 = LabeledComponent.create(new TextFieldWithBrowseButton(), "Enter the name of the client class");
        LabeledComponent<TextFieldWithBrowseButton> labeledField3 = LabeledComponent.create(new TextFieldWithBrowseButton(), "Enter the name of the postclient to generate");
        LabeledComponent<TextFieldWithBrowseButton> labeledField4 = LabeledComponent.create(new TextFieldWithBrowseButton(), "Enter the output directory");

        mainPanel.add(labeledField1);
        mainPanel.add(labeledField2);
        mainPanel.add(labeledField3);
        mainPanel.add(labeledField4);

        return mainPanel;
    }
}