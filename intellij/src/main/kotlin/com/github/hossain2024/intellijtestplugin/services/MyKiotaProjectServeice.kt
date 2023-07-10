package com.github.hossain2024.intellijtestplugin.services

import com.intellij.openapi.components.Service
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.project.Project
import com.github.hossain2024.intellijtestplugin.MyBundle
import java.util.*

@Service(Service.Level.PROJECT)
class MyKiotaProjectService(project: Project) {

    init {
        thisLogger().info(MyBundle.message("projectService", project.name))
        thisLogger().warn("Don't forget to remove all non-needed sample code files with their corresponding registration entries in `plugin.xml`.")
    }

    fun getRandomNumber() = (1..100).random()

    fun calculatePowerOfTwo(exponent: Int): Long {
        require(exponent >= 0) { "Exponent must be non-negative" }
        return 1L shl exponent
    }

    fun getoutput(){

    }





}
