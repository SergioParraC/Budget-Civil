import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { Permission } from '../../../core/auth/permissions';
import { ProjectBudget } from '../../../core/models/project.model';
import {
  WbsActivityResponse,
  WbsItemApuResponse,
  WbsNode,
  WbsTreeResponse
} from '../../../core/models/wbs.model';
import { ProjectContextService } from '../../../core/services/project-context.service';
import { ProjectService } from '../../../core/services/project.service';
import { WbsService } from '../../../core/services/wbs.service';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Edt } from './edt';

const projectId = 'project-1';

const itemNode = (): WbsNode =>
  ({
    id: 'item-1',
    projectId,
    nodeType: 'ITEM',
    code: '1.1',
    parentChapterId: null,
    name: null,
    quantityWork: 5,
    orderIndex: 0,
    rowVersion: 'item-row-version',
    lastModifiedBy: 'user-1',
    lastModifiedAt: '2026-11-21T00:00:00Z',
    apu: {
      id: 'apu-1',
      status: 'COMPLETO',
      description: 'APU de prueba',
      unit: 'UND',
      unitPrice: 10,
      rowVersion: 'apu-row-version'
    },
    unitCost: 10,
    directCost: 50,
    children: []
  }) as WbsNode;

describe('Edt edición en línea de la cantidad', () => {
  let fixture: ComponentFixture<Edt>;
  let component: Edt;
  let canWriteApu = true;
  let wbsService: {
    getTree: jasmine.Spy;
    getActivity: jasmine.Spy;
    updateItem: jasmine.Spy;
  };
  let projectService: { getBudget: jasmine.Spy };
  let messageService: { add: jasmine.Spy };

  beforeEach(() => {
    canWriteApu = true;
    wbsService = {
      getTree: jasmine
        .createSpy('getTree')
        .and.returnValue(of({ projectId, nodes: [] } as WbsTreeResponse)),
      getActivity: jasmine
        .createSpy('getActivity')
        .and.returnValue(of({ projectId, participants: [], lastActivity: null } as WbsActivityResponse)),
      updateItem: jasmine.createSpy('updateItem').and.callFake(
        (requestProjectId: string, itemId: string) =>
          of({
            id: itemId,
            projectId: requestProjectId,
            quantityWork: 12.5,
            rowVersion: 'item-row-version-2'
          } as WbsItemApuResponse)
      )
    };
    projectService = {
      getBudget: jasmine
        .createSpy('getBudget')
        .and.returnValue(of({ projectId, quantityDecimals: 7 } as ProjectBudget))
    };
    messageService = { add: jasmine.createSpy('add') };

    TestBed.configureTestingModule({
      imports: [Edt],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === 'id' ? projectId : null)
              }
            }
          }
        },
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } },
        {
          provide: AuthService,
          useValue: {
            can: (permission: string) => permission !== Permission.apusWrite || canWriteApu
          }
        },
        { provide: WbsService, useValue: wbsService },
        { provide: ProjectService, useValue: projectService },
        {
          provide: ProjectContextService,
          useValue: {
            project: signal(null),
            unavailable: signal(false),
            load: jasmine.createSpy('load'),
            goToList: jasmine.createSpy('goToList')
          }
        },
        { provide: MessageService, useValue: messageService },
        { provide: ConfirmationService, useValue: { confirm: jasmine.createSpy('confirm') } }
      ]
    });

    fixture = TestBed.createComponent(Edt);
    component = fixture.componentInstance;
  });

  it('guarda la cantidad editada y refresca árbol y presupuesto', () => {
    const node = itemNode();
    component.nodes.set([node]);

    component.onQuantityChange(node, 12.5);
    component.onQuantityBlur(node);

    expect(wbsService.updateItem).toHaveBeenCalledOnceWith(projectId, node.id, {
      quantityWork: 12.5,
      rowVersion: 'item-row-version'
    });
    expect(projectService.getBudget).toHaveBeenCalled();
    expect(wbsService.getTree).toHaveBeenCalled();
    expect(component.savingQuantityId()).toBeNull();
    expect(component.quantityValue(node)).toBe(12.5);
  });

  it('confirma la cantidad con Enter', () => {
    const node = itemNode();
    component.nodes.set([node]);

    component.onQuantityChange(node, 8);
    component.onQuantityKeyDown(new KeyboardEvent('keydown', { key: 'Enter' }), node);

    expect(wbsService.updateItem).toHaveBeenCalledOnceWith(projectId, node.id, {
      quantityWork: 8,
      rowVersion: 'item-row-version'
    });
  });

  it('no guarda si la cantidad está vacía o es negativa', () => {
    const node = itemNode();
    component.nodes.set([node]);

    component.onQuantityChange(node, null);
    component.onQuantityBlur(node);
    expect(wbsService.updateItem).not.toHaveBeenCalled();
    expect(messageService.add).toHaveBeenCalled();

    messageService.add.calls.reset();
    component.onQuantityChange(node, -1);
    component.onQuantityBlur(node);
    expect(wbsService.updateItem).not.toHaveBeenCalled();
    expect(messageService.add).toHaveBeenCalled();
  });

  it('no guarda si la cantidad no cambió', () => {
    const node = itemNode();
    component.nodes.set([node]);

    component.onQuantityChange(node, node.quantityWork);
    component.onQuantityBlur(node);

    expect(wbsService.updateItem).not.toHaveBeenCalled();
  });

  it('descarta el borrador con Escape', () => {
    const node = itemNode();
    component.nodes.set([node]);

    component.onQuantityChange(node, 99);
    component.onQuantityKeyDown(new KeyboardEvent('keydown', { key: 'Escape' }), node);

    expect(component.quantityValue(node)).toBe(node.quantityWork);
    expect(wbsService.updateItem).not.toHaveBeenCalled();
  });

  it('no permite editar sin APUS_WRITE', () => {
    canWriteApu = false;
    const node = itemNode();
    component.nodes.set([node]);

    component.onQuantityChange(node, 10);
    component.onQuantityBlur(node);

    expect(component.canEditQuantity()).toBeFalse();
    expect(wbsService.updateItem).not.toHaveBeenCalled();
  });
});
