import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Connector, ConnectorRequest, ExternalSystem, ExternalSystemRequest, ListResponse,
  Workflow, WorkflowDetails, WorkflowRequest
} from './models';

@Injectable({ providedIn: 'root' })
export class HubApiService {
  private readonly http = inject(HttpClient);
  private readonly root = '/api';

  getSystems(): Observable<ListResponse<ExternalSystem>> {
    return this.http.get<ListResponse<ExternalSystem>>(`${this.root}/external-systems`);
  }

  createSystem(payload: ExternalSystemRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.root}/external-systems`, payload);
  }

  updateSystem(id: string, payload: ExternalSystemRequest): Observable<void> {
    return this.http.put<void>(`${this.root}/external-systems/${id}`, payload);
  }

  setSystemActive(id: string, active: boolean): Observable<void> {
    return this.http.post<void>(`${this.root}/external-systems/${id}/${active ? 'activate' : 'deactivate'}`, {});
  }

  getConnectors(): Observable<ListResponse<Connector>> {
    return this.http.get<ListResponse<Connector>>(`${this.root}/connectors`);
  }

  createConnector(payload: ConnectorRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.root}/connectors`, payload);
  }

  updateConnector(id: string, payload: ConnectorRequest): Observable<void> {
    return this.http.put<void>(`${this.root}/connectors/${id}`, payload);
  }

  setConnectorActive(id: string, active: boolean): Observable<void> {
    return this.http.post<void>(`${this.root}/connectors/${id}/${active ? 'activate' : 'deactivate'}`, {});
  }

  getWorkflows(): Observable<ListResponse<Workflow>> {
    return this.http.get<ListResponse<Workflow>>(`${this.root}/workflows`);
  }

  getWorkflow(id: string): Observable<WorkflowDetails> {
    return this.http.get<WorkflowDetails>(`${this.root}/workflows/${id}`);
  }

  createWorkflow(payload: WorkflowRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.root}/workflows`, payload);
  }

  updateWorkflow(id: string, payload: WorkflowRequest): Observable<void> {
    return this.http.put<void>(`${this.root}/workflows/${id}`, payload);
  }

  activateWorkflow(id: string): Observable<void> {
    return this.http.post<void>(`${this.root}/workflows/${id}/activate`, {});
  }
}
