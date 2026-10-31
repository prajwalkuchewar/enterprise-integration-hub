import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewChecked, Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { HubApiService } from './hub-api.service';
import {
  authenticationLabel, authenticationTypes, Connector, ConnectorRequest, environments,
  environmentLabel, ExternalSystem, ExternalSystemRequest, protocols, protocolLabel,
  statusLabel, Workflow, WorkflowDetails, WorkflowRequest, WorkflowStep
} from './models';

type Section = 'dashboard' | 'systems' | 'connectors' | 'workflows';
type ModalKind = 'system' | 'connector' | 'workflow' | null;

@Component({
  selector: 'eih-root',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit, AfterViewChecked {
  private readonly api = inject(HubApiService);
  private readonly router = inject(Router);
  private refreshGeneration = 0;
  @ViewChild('modalDialog') private modalDialog?: ElementRef<HTMLDialogElement>;

  readonly environments = environments;
  readonly protocols = protocols;
  readonly authenticationTypes = authenticationTypes;
  readonly environmentLabel = environmentLabel;
  readonly protocolLabel = protocolLabel;
  readonly authenticationLabel = authenticationLabel;
  readonly statusLabel = statusLabel;

  section: Section = 'dashboard';
  systems: ExternalSystem[] = [];
  connectors: Connector[] = [];
  workflows: Workflow[] = [];
  loading = true;
  connectionState: 'connecting' | 'connected' | 'offline' = 'connecting';
  saving = false;
  modal: ModalKind = null;
  editingId: string | null = null;
  search = '';
  notice = '';
  apiError = '';

  systemForm: ExternalSystemRequest = { name: '', description: '', environment: 1 };
  connectorForm: ConnectorRequest = {
    name: '', description: '', externalSystemId: '', baseUrl: 'https://',
    protocol: 1, authenticationType: 1, timeoutSeconds: 30
  };
  workflowForm: WorkflowRequest = {
    name: '', sourceConnectorId: '', triggerEvent: '',
    steps: [{ destinationConnectorId: '', executionOrder: 1 }]
  };

  ngOnInit(): void {
    this.router.events.pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        const path = this.router.parseUrl(event.urlAfterRedirects).root.children['primary']?.segments[0]?.path as Section | undefined;
        this.section = path === 'systems' || path === 'connectors' || path === 'workflows' ? path : 'dashboard';
        this.search = '';
      });
    this.refresh();
  }

  ngAfterViewChecked(): void {
    if (this.modal && this.modalDialog && !this.modalDialog.nativeElement.open) {
      this.modalDialog.nativeElement.showModal();
    }
  }

  refresh(): void {
    const generation = ++this.refreshGeneration;
    this.loading = true;
    this.connectionState = 'connecting';
    this.apiError = '';
    let remaining = 3;
    const finished = (): void => {
      if (generation !== this.refreshGeneration) return;
      remaining -= 1;
      if (remaining === 0) {
        this.loading = false;
        this.connectionState = this.apiError ? 'offline' : 'connected';
      }
    };

    this.api.getSystems().subscribe({
      next: (response) => {
        if (generation !== this.refreshGeneration) return;
        this.systems = response.items ?? [];
        finished();
      },
      error: (error: unknown) => {
        if (generation !== this.refreshGeneration) return;
        this.apiError = this.errorMessage(error);
        this.systems = [];
        finished();
      }
    });
    this.api.getConnectors().subscribe({
      next: (response) => {
        if (generation !== this.refreshGeneration) return;
        this.connectors = response.items ?? [];
        finished();
      },
      error: (error: unknown) => {
        if (generation !== this.refreshGeneration) return;
        this.apiError = this.errorMessage(error);
        this.connectors = [];
        finished();
      }
    });
    this.api.getWorkflows().subscribe({
      next: (response) => {
        if (generation !== this.refreshGeneration) return;
        this.workflows = response.items ?? [];
        finished();
      },
      error: (error: unknown) => {
        if (generation !== this.refreshGeneration) return;
        this.apiError = this.errorMessage(error);
        this.workflows = [];
        finished();
      }
    });
  }

  get activeSystems(): number { return this.systems.filter((item) => item.status === 1).length; }
  get activeConnectors(): number { return this.connectors.filter((item) => item.status === 2).length; }
  get activeWorkflows(): number { return this.workflows.filter((item) => item.status === 1).length; }
  get pageTitle(): string {
    return ({ dashboard: 'Overview', systems: 'External systems', connectors: 'Connectors', workflows: 'Workflows' })[this.section];
  }
  get todayLabel(): string {
    return new Intl.DateTimeFormat('en', { weekday: 'long', month: 'long', day: 'numeric' })
      .format(new Date()).toUpperCase();
  }
  get filteredSystems(): ExternalSystem[] {
    const query = this.search.trim().toLowerCase();
    return this.systems.filter((item) => `${item.name} ${item.description} ${this.environmentLabel(item.environment)}`.toLowerCase().includes(query));
  }
  get filteredConnectors(): Connector[] {
    const query = this.search.trim().toLowerCase();
    return this.connectors.filter((item) => `${item.name} ${item.description} ${item.baseUrl} ${this.systemName(item.externalSystemId)}`.toLowerCase().includes(query));
  }
  get filteredWorkflows(): Workflow[] {
    const query = this.search.trim().toLowerCase();
    return this.workflows.filter((item) => `${item.name} ${item.triggerEvent} ${this.connectorName(item.sourceConnectorId)}`.toLowerCase().includes(query));
  }
  get activeSystemOptions(): ExternalSystem[] { return this.systems.filter((item) => item.status === 1); }
  get activeConnectorOptions(): Connector[] { return this.connectors.filter((item) => item.status === 2); }

  openCreate(kind: Exclude<ModalKind, null>): void {
    this.modal = kind;
    this.editingId = null;
    this.apiError = '';
    if (kind === 'system') this.systemForm = { name: '', description: '', environment: 1 };
    if (kind === 'connector') this.connectorForm = {
      name: '', description: '', externalSystemId: this.activeSystemOptions[0]?.id ?? '',
      baseUrl: 'https://', protocol: 1, authenticationType: 1, timeoutSeconds: 30
    };
    if (kind === 'workflow') this.workflowForm = {
      name: '', sourceConnectorId: this.activeConnectorOptions[0]?.id ?? '', triggerEvent: '',
      steps: [{ destinationConnectorId: '', executionOrder: 1 }]
    };
  }

  editSystem(system: ExternalSystem): void {
    this.apiError = '';
    this.modal = 'system';
    this.editingId = system.id;
    this.systemForm = { name: system.name, description: system.description, environment: system.environment };
  }

  editConnector(connector: Connector): void {
    this.apiError = '';
    this.modal = 'connector';
    this.editingId = connector.id;
    this.connectorForm = {
      name: connector.name, description: connector.description, externalSystemId: connector.externalSystemId,
      baseUrl: connector.baseUrl, protocol: connector.protocol,
      authenticationType: connector.authenticationType, timeoutSeconds: connector.timeoutSeconds
    };
  }

  editWorkflow(workflow: Workflow): void {
    this.apiError = '';
    this.api.getWorkflow(workflow.id).subscribe({
      next: (details: WorkflowDetails) => {
        this.modal = 'workflow';
        this.editingId = workflow.id;
        this.workflowForm = {
          name: details.name,
          sourceConnectorId: details.sourceConnectorId,
          triggerEvent: details.triggerEvent,
          steps: details.steps.map((step) => ({ ...step }))
        };
      },
      error: (error: unknown) => this.apiError = this.errorMessage(error)
    });
  }

  closeModal(): void {
    if (this.saving) return;
    this.modalDialog?.nativeElement.close();
    this.modal = null;
    this.editingId = null;
  }

  onModalCancel(event: Event): void {
    if (this.saving) {
      event.preventDefault();
    }
  }

  onModalClosed(): void {
    this.modal = null;
    this.editingId = null;
  }

  sourceConnectorChanged(sourceConnectorId: string): void {
    this.workflowForm.sourceConnectorId = sourceConnectorId;
    this.workflowForm.steps = this.workflowForm.steps.map((step) =>
      step.destinationConnectorId === sourceConnectorId
        ? { ...step, destinationConnectorId: '' }
        : step
    );
  }

  saveSystem(): void {
    this.saving = true;
    if (this.editingId) {
      this.api.updateSystem(this.editingId, this.systemForm).subscribe({
        next: () => this.completeSave('External system updated.'),
        error: (error: unknown) => this.saveFailed(error)
      });
    } else {
      this.api.createSystem(this.systemForm).subscribe({
        next: () => this.completeSave('External system created.'),
        error: (error: unknown) => this.saveFailed(error)
      });
    }
  }

  saveConnector(): void {
    this.saving = true;
    const payload = { ...this.connectorForm };
    if (this.editingId) {
      this.api.updateConnector(this.editingId, payload).subscribe({
        next: () => this.completeSave('Connector updated.'),
        error: (error: unknown) => this.saveFailed(error)
      });
    } else {
      this.api.createConnector(payload).subscribe({
        next: () => this.completeSave('Connector created.'),
        error: (error: unknown) => this.saveFailed(error)
      });
    }
  }

  saveWorkflow(): void {
    this.saving = true;
    const payload = { ...this.workflowForm, steps: this.workflowForm.steps.map((step, index) => ({ ...step, executionOrder: index + 1 })) };
    if (this.editingId) {
      this.api.updateWorkflow(this.editingId, payload).subscribe({
        next: () => this.completeSave('Workflow updated.'),
        error: (error: unknown) => this.saveFailed(error)
      });
    } else {
      this.api.createWorkflow(payload).subscribe({
        next: () => this.completeSave('Workflow created.'),
        error: (error: unknown) => this.saveFailed(error)
      });
    }
  }

  addWorkflowStep(): void {
    this.workflowForm.steps = [...this.workflowForm.steps, { destinationConnectorId: '', executionOrder: this.workflowForm.steps.length + 1 }];
  }

  removeWorkflowStep(index: number): void {
    if (this.workflowForm.steps.length > 1) this.workflowForm.steps = this.workflowForm.steps.filter((_, position) => position !== index);
  }

  toggleSystem(system: ExternalSystem): void {
    this.api.setSystemActive(system.id, system.status !== 1).subscribe({
      next: () => { this.toast(`System ${system.status === 1 ? 'deactivated' : 'activated'}.`); this.refresh(); },
      error: (error: unknown) => this.apiError = this.errorMessage(error)
    });
  }

  toggleConnector(connector: Connector): void {
    this.api.setConnectorActive(connector.id, connector.status !== 2).subscribe({
      next: () => { this.toast(`Connector ${connector.status === 2 ? 'deactivated' : 'activated'}.`); this.refresh(); },
      error: (error: unknown) => this.apiError = this.errorMessage(error)
    });
  }

  activateWorkflow(workflow: Workflow): void {
    this.api.activateWorkflow(workflow.id).subscribe({
      next: () => { this.toast('Workflow activated.'); this.refresh(); },
      error: (error: unknown) => this.apiError = this.errorMessage(error)
    });
  }

  systemName(id: string): string { return this.systems.find((item) => item.id === id)?.name ?? 'Unknown system'; }
  connectorName(id: string): string { return this.connectors.find((item) => item.id === id)?.name ?? 'Unknown connector'; }
  connectorCount(systemId: string): number { return this.connectors.filter((item) => item.externalSystemId === systemId).length; }
  trackById(_index: number, item: { id: string }): string { return item.id; }
  trackByIndex(index: number): number { return index; }
  statusClass(status: string): string { return status.toLowerCase().replaceAll(' ', '-'); }

  private completeSave(message: string): void {
    this.saving = false;
    this.closeModal();
    this.toast(message);
    this.refresh();
  }

  private saveFailed(error: unknown): void {
    this.saving = false;
    this.apiError = this.errorMessage(error);
  }

  private toast(message: string): void {
    this.notice = message;
    window.setTimeout(() => { if (this.notice === message) this.notice = ''; }, 3600);
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { message?: string; detail?: string; title?: string } | null;
      if (body?.message || body?.detail) return body.message ?? body.detail ?? 'The request could not be completed.';
      if (error.status === 0) return 'Cannot reach the API. Start the backend and SQL Server, then try again.';
      return body?.title ?? `Request failed (${error.status}).`;
    }
    return 'Something went wrong. Please try again.';
  }
}
