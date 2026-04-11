import axios from "axios";
import type {
  AnalysisResult,
  MigrationRoadmap,
  Project,
  TransformResult,
} from "../types";

const api = axios.create({ baseURL: "/api" });

// Projects
export const getProjects = () =>
  api.get<Project[]>("/projects").then((r) => r.data);

export const getProject = (id: string) =>
  api.get<Project>(`/projects/${id}`).then((r) => r.data);

export const createProject = (name: string) =>
  api.post<Project>("/projects", { name }).then((r) => r.data);

export const deleteProject = (id: string) =>
  api.delete(`/projects/${id}`);

// Analysis
export const runAnalysis = (projectId: string, rootPath: string) =>
  api
    .post<AnalysisResult>(`/projects/${projectId}/analysis`, { rootPath })
    .then((r) => r.data);

export const getAnalysis = (projectId: string) =>
  api
    .get<AnalysisResult>(`/projects/${projectId}/analysis`)
    .then((r) => r.data);

// Roadmap
export const generateRoadmap = (projectId: string) =>
  api
    .post<MigrationRoadmap>(`/projects/${projectId}/roadmap/generate`)
    .then((r) => r.data);

export const getRoadmap = (projectId: string) =>
  api
    .get<MigrationRoadmap>(`/projects/${projectId}/roadmap`)
    .then((r) => r.data);

// Transform
export const previewTransform = (
  projectId: string,
  filePath: string,
  targetFramework: string
) =>
  api
    .post<TransformResult>(`/projects/${projectId}/transform/preview`, {
      filePath,
      targetFramework,
    })
    .then((r) => r.data);

export const applyTransform = (
  projectId: string,
  filePath: string,
  targetFramework: string
) =>
  api
    .post<TransformResult>(`/projects/${projectId}/transform`, {
      filePath,
      targetFramework,
    })
    .then((r) => r.data);
