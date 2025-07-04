export interface ApiManifest {
    apiDependencies: Record<string, ApiDependency>;
}
export interface ApiDependency {
    apiDescriptionUrl: string;
}