package com.github.hossain2024.intellijtestplugin.toolWindow;

import com.github.hossain2024.intellijtestplugin.MyBundle;
import com.github.hossain2024.intellijtestplugin.services.KiotaProjectService;
import com.intellij.openapi.components.ServiceManager;
import com.intellij.openapi.diagnostic.Logger;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.wm.ToolWindow;
import com.intellij.openapi.wm.ToolWindowFactory;
import com.intellij.ui.components.JBLabel;
import com.intellij.ui.components.JBPanel;
import com.intellij.ui.content.Content;
import com.intellij.ui.content.ContentFactory;
import org.jetbrains.annotations.NotNull;

import java.awt.GridBagConstraints;
import java.awt.GridBagLayout;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import javax.swing.JButton;
import javax.swing.JComponent;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JTextField;

public class MyKiotaToolWindowFactory implements ToolWindowFactory {

    public MyKiotaToolWindowFactory() {
        Logger.getInstance(MyToolWindowFactory.class).warn("Don't forget to remove all non-needed sample code files with their corresponding registration entries in `plugin.xml`.");
    }

    @Override
    public void createToolWindowContent(Project project, ToolWindow toolWindow) {
        MyToolWindow myToolWindow = new MyToolWindow(toolWindow);
        ContentFactory contentFactory = ContentFactory.getInstance();
        Content quote = contentFactory.createContent(myToolWindow.getQuote(), null, false);
        toolWindow.getContentManager().addContent(quote);
    }

    public boolean shouldBeAvailable(Project project) {
        return true;
    }

    private static class MyToolWindow {
        private KiotaProjectService service;

        public MyToolWindow(@NotNull ToolWindow toolWindow) {
            Project project = toolWindow.getProject();
            service = ServiceManager.getService(project, KiotaProjectService.class);
        }

        public JComponent getQuote() {
            JBPanel<JBPanel<?>> mainPanel = new JBPanel<>();
            mainPanel.setLayout(new GridBagLayout());

            GridBagConstraints constraints = new GridBagConstraints();
            constraints.gridx = 0;
            constraints.gridy = 0;
            constraints.anchor = GridBagConstraints.NORTH;

            // Prompt panel
            JBPanel<JBPanel<?>> promptPanel = new JBPanel<>();
            JBLabel promptLabel = new JBLabel(MyBundle.message("Label", ""));
            promptPanel.add(promptLabel);
            mainPanel.add(promptPanel, constraints);

            // Input panel
            constraints.gridy = 1;
            JBPanel<JBPanel<?>> inputPanel = new JBPanel<>();
            JTextField textField = new JTextField();
            JBLabel resultLabel = new JBLabel();
            JButton valueButton = new JButton(MyBundle.message("value"));
            valueButton.addActionListener(new ActionListener() {
                @Override
                public void actionPerformed(ActionEvent e) {
                    try {
                        int inputNumber = Integer.parseInt(textField.getText());
                        int powerOfTwo = (int) service.calculatePowerOfTwo(inputNumber);
                        resultLabel.setText(MyBundle.message("resutmessage", powerOfTwo));
                    } catch (NumberFormatException ex) {
                        resultLabel.setText("Invalid input!");
                    }
                }
            });
            inputPanel.add(textField);
            inputPanel.add(valueButton);
            mainPanel.add(inputPanel, constraints);

            // Result panel
            constraints.gridy = 2;
            JBPanel<JBPanel<?>> resultPanel = new JBPanel<>();
            resultPanel.add(resultLabel);
            mainPanel.add(resultPanel, constraints);

            return mainPanel;
        }


    }
}