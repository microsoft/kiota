// swift-tools-version:5.0
import PackageDescription

let package = Package(
    name: "MicrosoftKiotaAbstractions",
    products: [
        .library(name: "MicrosoftKiotaAbstractions", targets: ["MicrosoftKiotaAbstractions"])
    ],
    dependencies: [
        .package(url: "https://github.com/kylef/URITemplate.swift.git", from: "3.0.0")
    ],
    targets: [
        .target(name: "MicrosoftKiotaAbstractions", dependencies: ["URITemplate"]),
        .testTarget(name: "MicrosoftKiotaAbstractionsTests", dependencies: ["MicrosoftKiotaAbstractions"])
    ],
    swiftLanguageVersions: [.v5]
)