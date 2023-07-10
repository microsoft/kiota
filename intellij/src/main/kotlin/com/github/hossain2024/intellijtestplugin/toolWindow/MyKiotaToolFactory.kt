package com.github.hossain2024.intellijtestplugin.toolWindow

import com.github.hossain2024.intellijtestplugin.MyBundle
import com.github.hossain2024.intellijtestplugin.services.MyKiotaProjectService
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.LabeledComponent
import com.intellij.openapi.ui.TextFieldWithBrowseButton

import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ToolWindowFactory
import com.intellij.ui.JBColor
import com.intellij.ui.components.JBLabel
import com.intellij.ui.components.JBPanel
import com.intellij.ui.components.JBTextField
import com.intellij.ui.components.panels.VerticalLayout
import com.intellij.ui.content.ContentFactory
import com.intellij.ui.layout.LCFlags
import java.awt.*
import javax.swing.*


class MyKiotaToolFactory : ToolWindowFactory {

    init {
        thisLogger().warn("Don't forget to remove all non-needed sample code files with their corresponding registration entries in `plugin.xml`.")
    }

    override fun createToolWindowContent(project: Project, toolWindow: ToolWindow) {
        val myToolWindow = MyToolWindow(toolWindow)
        //val content = ContentFactory.getInstance().createContent(myToolWindow.getContent(), null, false)
        // val content2= ContentFactory.getInstance().createContent(myToolWindow.getContent2(), null, false)
        //val power_of_two = ContentFactory.getInstance().createContent(myToolWindow.getpowerof2(), null, false)
        //val input =  ContentFactory.getInstance().createContent(myToolWindow.getinput(), null, false)
        val input =  ContentFactory.getInstance().createContent(myToolWindow.Addpanel(), null, false)


        // toolWindow.contentManager.addContent(power_of_two)
        //toolWindow.contentManager.addContent(input)
        toolWindow.contentManager.addContent(input)


    }

    override fun shouldBeAvailable(project: Project) = true

    class MyToolWindow(toolWindow: ToolWindow) {

        private val service = toolWindow.project.service<MyKiotaProjectService>()
        val ParentPanel = JBPanel<JBPanel<*>>()


        fun Addpanel(): JComponent {
            ParentPanel.layout = BorderLayout()
            ParentPanel.add(getinput(), BorderLayout.CENTER)

            return ParentPanel
        }


        //Make a inout field panel

        fun getinput(): JComponent {
            val mainPanel = JBPanel<JBPanel<*>>()
            mainPanel.layout = VerticalLayout(10)

            val labeledField1 = LabeledComponent.create(TextFieldWithBrowseButton(), "Enter the YAML file Path")
            val labeledField2 = LabeledComponent.create(TextFieldWithBrowseButton(), "Field 2:")
            val labeledField3 = LabeledComponent.create(TextFieldWithBrowseButton(), "Field 3:")
            val labeledField4 = LabeledComponent.create(TextFieldWithBrowseButton(), "Field 4:")

            mainPanel.add(labeledField1)
            mainPanel.add(labeledField2)
            mainPanel.add(labeledField3)
            mainPanel.add(labeledField4)

            return mainPanel
        }






    }
    }