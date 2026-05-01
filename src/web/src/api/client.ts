import axios from "axios";
import type {
  AnalysisResult,
  MigrationRoadmap,
  Project,
  TransformResult,
} from "../types";

const api = axios.create({ baseURL: `${import.meta.env.VITE_API_URL ?? ""}/api` });

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

export const uploadAndAnalyze = (projectId: string, file: File) => {
  const form = new FormData();
  form.append("file", file);
  return api
    .post<AnalysisResult>(`/projects/${projectId}/analysis/upload`, form, {
      headers: { "Content-Type": "multipart/form-data" },
    })
    .then((r) => r.data);
};

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

// Download
export const downloadTransformed = (projectId: string) =>
  api.get(`/projects/${projectId}/download`, { responseType: "blob" }).then((r) => {
    const url = URL.createObjectURL(r.data);
    const a = document.createElement("a");
    a.href = url;
    a.download = `${projectId}_transformed.zip`;
    a.click();
    URL.revokeObjectURL(url);
  });

export const downloadFile = (projectId: string, filePath: string) =>
  api.get(`/projects/${projectId}/download/file`, { params: { path: filePath }, responseType: "blob" }).then((r) => {
    const url = URL.createObjectURL(r.data);
    const a = document.createElement("a");
    a.href = url;
    a.download = filePath.split("/").pop() ?? "transformed.cs";
    a.click();
    URL.revokeObjectURL(url);
  });

// Transform
export const applyContent = (projectId: string, filePath: string, content: string) =>
  api
    .post(`/projects/${projectId}/transform/apply-content`, { filePath, content })
    .then((r) => r.data);

export const modernizeFile = (projectId: string, filePath: string, targetFramework: string, apiKey?: string) =>
  api
    .post<TransformResult>(
      `/projects/${projectId}/transform/modernize`,
      { filePath, targetFramework },
      apiKey ? { headers: { "X-Anthropic-Key": apiKey } } : undefined
    )
    .then((r) => r.data);

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
