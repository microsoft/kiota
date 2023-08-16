package toolWindow;
import com.intellij.notification.Notification;
import com.intellij.notification.NotificationType;
import com.intellij.openapi.project.ProjectManager;
import com.intellij.openapi.ui.ComboBox;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.ui.LabeledComponent;
import com.intellij.openapi.ui.TextFieldWithBrowseButton;
import com.intellij.openapi.wm.ToolWindow;
import com.intellij.ui.components.JBPanel;
import com.intellij.ui.components.panels.VerticalLayout;
import services.GenerateClientHandler;
import services.KiotaGenerationLanguage;
import services.VersionHandler;
import javax.swing.*;
import java.awt.*;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.io.File;
import java.io.IOException;
import static com.intellij.find.actions.FindInPathAction.NOTIFICATION_GROUP;

public class MyToolWindow {
    private final JBPanel<JBPanel<?>> ParentPanel;
    JPanel progressBarPanel = new JPanel();

    VersionHandler versionHandler;
    GenerateClientHandler generateClientHandler;

    public MyToolWindow(ToolWindow toolWindow) {
        ParentPanel = new JBPanel<>();
        versionHandler = new VersionHandler();
        generateClientHandler = new GenerateClientHandler();

    }

    /**
     * @return parentpanel
     * @throws IOException
     */
    public JComponent Addpanel() throws IOException {
        ParentPanel.setLayout(new BorderLayout());
        ParentPanel.add(getInput(), BorderLayout.CENTER);
        ParentPanel.add(getversion(), BorderLayout.SOUTH);
        return ParentPanel;
    }

    /**
     * This method displays version of kiota.
     * @return versionpanel
     * @throws IOException
     */
    public JComponent getversion() throws IOException {
        JBPanel<JBPanel<?>> versionPanel = new JBPanel<>();
        versionPanel.setLayout(new BorderLayout());
        // Create a label to display kiota version
        JLabel versionLabel = new JLabel("Kiota version:" +versionHandler.getVersion());
        versionPanel.add(versionLabel, BorderLayout.CENTER);
        return versionPanel;
    }

    /**
     * this method sets the client(UI) to get input from user and creates button with their functions
     * @return panel
     */
    public JComponent getInput() {
        JBPanel<JBPanel<?>> mainPanel = new JBPanel<>();
        mainPanel.setLayout(new VerticalLayout(10));

        // Create the TextFieldWithBrowseButton components
        TextFieldWithBrowseButton yamlFilePathField = new TextFieldWithBrowseButton();
        TextFieldWithBrowseButton outputpath = new TextFieldWithBrowseButton();
        JTextField clientClassField = new JTextField();
        JTextField postClientNameField = new JTextField();

        // Input labels
        LabeledComponent<TextFieldWithBrowseButton> yamlFilePath_label = LabeledComponent.create(yamlFilePathField, "Enter a path to an openAPI description");
        LabeledComponent<TextFieldWithBrowseButton> outputpath_Label = LabeledComponent.create(outputpath, "Enter an output path ");
        JLabel languageLabel = new JLabel("Pick a language");
        LabeledComponent<JTextField> clientclassname_label = LabeledComponent.create(clientClassField, "Enter a name for the client class");
        LabeledComponent<JTextField> clientclassnameSpace_label = LabeledComponent.create(postClientNameField, "Enter the name of the client class namespace");


        JComboBox<KiotaGenerationLanguage> languageComboBox = new ComboBox<>(KiotaGenerationLanguage.values());
        languageComboBox.setSelectedItem(KiotaGenerationLanguage.Java);
        String[] include = new String[0]; //empty string
        String[] exclude = new String[0]; //empty String

        // Add an action listener to the  generate button to perform an action when clicked
        JButton generateButton = new JButton("Generate");
        JProgressBar progressBar = new JProgressBar();
        progressBar.setIndeterminate(true);
        JPanel generateButtonPanel = new JPanel(new FlowLayout(FlowLayout.CENTER));
        generateButtonPanel.add(generateButton); // Add the generate button to the panel


        progressBarPanel.setLayout(new FlowLayout(FlowLayout.CENTER)); // changed this
        progressBar.setName("generate");
        progressBarPanel.add(progressBar);
        generateButtonPanel.add(progressBarPanel);
        progressBar.setVisible(false);
        generateButton.addActionListener(new ActionListener() {  // generatebutton action listener
            @Override
            public void actionPerformed(ActionEvent e) {
                generateButton.setVisible(false);
                progressBar.setVisible(true);
                progressBarPanel.removeAll(); // Clear any previous components
                progressBarPanel.add(progressBar); // Add the progress bar

                // perform the generation in a separate thread
                SwingWorker<Void, Void> worker = new SwingWorker<Void, Void>() {
                    @Override
                    protected Void doInBackground() throws Exception {
                        String descriptionPath = yamlFilePathField.getText();
                        String output = outputpath.getText();
                        String clientClass = clientClassField.getText();
                        String clientClassNamespace = postClientNameField.getText();
                        KiotaGenerationLanguage selectedLanguage = (KiotaGenerationLanguage) languageComboBox.getSelectedItem();

                        generateClientHandler.generateclient(descriptionPath, output, selectedLanguage, include, exclude, clientClass, clientClassNamespace);

                        return null;
                    }
                    @Override
                    protected void done() {
                        // Generation is complete, re-enable the button
                        progressBarPanel.setVisible(true);
                        Object response = generateClientHandler.getResponse();
                        String responseString = response.toString();

                        Project[] openProjects = ProjectManager.getInstance().getOpenProjects();
                        if (openProjects.length > 0) {
                            Project currentProject = openProjects[0]; // Get the first open project

                            if (responseString.contains("error generating the client")) {
                                showStickyNotification(currentProject, "Error generating the client. " + responseString, NotificationType.ERROR);
                                progressBar.setVisible(false);
                                progressBar.setEnabled(false);
                                generateButton.setVisible(true);
                            } else if (responseString.contains("completed")) {
                                showStickyNotification(currentProject, "Generation completed successfully! ", NotificationType.INFORMATION);
                                progressBar.setVisible(false);
                                generateButton.setVisible(true);
                            }
                        }
                    }
                    private void showStickyNotification(Project project, String message, NotificationType type) {
                        Notification notification = NOTIFICATION_GROUP.createNotification(message, type);
                        notification.setImportant(true); // Make it sticky
                        notification.notify(project);
                    }


                };

                worker.execute();
            }
        });


        // Add the ActionListener to the  (yamlFilePathField) browse icon
        yamlFilePathField.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                JFileChooser fileChooser = new JFileChooser();
                fileChooser.setFileSelectionMode(JFileChooser.FILES_AND_DIRECTORIES);
                int returnValue = fileChooser.showOpenDialog(null);

                if (returnValue == JFileChooser.APPROVE_OPTION) {
                    File selectedFile = fileChooser.getSelectedFile();
                    String selectedPath = selectedFile.getAbsolutePath();
                    yamlFilePathField.setText(selectedPath);
                }
            }
        });
        // Add the ActionListener to the  (outputpath) browse icon
        outputpath.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                JFileChooser fileChooser = new JFileChooser();
                fileChooser.setFileSelectionMode(JFileChooser.FILES_AND_DIRECTORIES);
                int returnValue = fileChooser.showOpenDialog(null);

                if (returnValue == JFileChooser.APPROVE_OPTION) {
                    File selectedFile = fileChooser.getSelectedFile();
                    String selectedPath = selectedFile.getAbsolutePath();
                    outputpath.setText(selectedPath);
                }
            }
        });
        mainPanel.add(yamlFilePath_label);
        mainPanel.add(outputpath_Label);
        mainPanel.add(languageLabel);
        mainPanel.add(languageComboBox);
        mainPanel.add(clientclassname_label);
        mainPanel.add(clientclassnameSpace_label);
        mainPanel.add(generateButtonPanel);
        return mainPanel;
    }
}




