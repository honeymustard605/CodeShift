export type RiskLevel = "Low" | "Medium" | "High" | "Critical";

export interface DetectedProject {
  name: string;
  language: string;
  fileCount: number;
  rootPath: string;
  targetFramework: string;
}

export interface DependencyEdge {
  source: string;
  target: string;
  kind: string;
}

export interface RiskFlag {
  category: string;
  filePath: string;
  level: RiskLevel;
  description: string;
}

export interface AnalysisResult {
  language: string;
  projects: DetectedProject[];
  dependencies: DependencyEdge[];
  risks: RiskFlag[];
  totalFiles: number;
  totalLoc: number;
  analyzedAt: string;
}

export interface RoadmapTask {
  title: string;
  description: string;
  complexity: RiskLevel;
  fileReference?: string;
}

export interface RoadmapPhase {
  order: number;
  name: string;
  description: string;
  tasks: RoadmapTask[];
  estimatedWeeks: number;
}

export interface MigrationRoadmap {
  projectId: string;
  phases: RoadmapPhase[];
  estimatedWeeks: number;
  targetFramework: string;
  generatedAt: string;
}

export interface TransformResult {
  filePath: string;
  originalSource: string;
  transformedSource: string;
  targetFramework: string;
  appliedRules: string[];
  warnings: string[];
  success: boolean;
}

export interface Project {
  id: string;
  name: string;
  createdAt: string;
  status: string;
}
