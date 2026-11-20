import { Component, computed, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';

import { ApiError, resolveApiError } from '../../../../core/http/api-error';
import { WbsChapterOption, WbsNode } from '../../../../core/models/wbs.model';
import { WbsService } from '../../../../core/services/wbs.service';

/**
 * Alta y edición de un capítulo del EDT. El código se valida al salir del campo con
 * `GET /wbs/codes/validate` (`REQ-EDT-26`, `D-117`).
 */
@Component({
  selector: 'app-chapter-form-dialog',
  imports: [ReactiveFormsModule, DialogModule, ButtonModule, InputTextModule, SelectModule, MessageModule],
  templateUrl: './chapter-form-dialog.html',
  styleUrl: './chapter-form-dialog.scss'
})
export class ChapterFormDialog {
  private readonly wbsService = inject(WbsService);
  private readonly fb = inject(FormBuilder);

  readonly visible = model(false);
  readonly projectId = input.required<string>();
  readonly chapter = input<WbsNode | null>(null);
  readonly parentChapterId = input<string | null>(null);
  readonly chapterOptions = input<WbsChapterOption[]>([]);
  readonly saved = output<WbsNode>();
  readonly failed = output<ApiError>();

  readonly saving = signal(false);
  readonly codeState = signal<'idle' | 'checking' | 'valid' | 'invalid'>('idle');
  readonly codeMessage = signal<string | null>(null);

  readonly isEdit = computed(() => this.chapter() !== null);

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(10)]],
    name: ['', [Validators.required, Validators.maxLength(255)]],
    parentChapterId: ['']
  });

  readonly parentOptions = computed(() => [
    { id: '', label: 'Raíz' },
    ...this.chapterOptions().map((chapter) => ({
      id: chapter.id,
      label: `${chapter.code} · ${chapter.name}`
    }))
  ]);

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      untracked(() => this.reset());
    });
  }

  private reset(): void {
    const chapter = this.chapter();
    this.form.reset({
      code: chapter?.code ?? '',
      name: chapter?.name ?? '',
      parentChapterId: chapter?.parentChapterId ?? this.parentChapterId() ?? ''
    });
    this.codeState.set('idle');
    this.codeMessage.set(null);
  }

  onCodeBlur(): void {
    const code = this.form.controls.code.value.trim();
    if (!code) {
      this.codeState.set('invalid');
      this.codeMessage.set('El código es obligatorio.');
      return;
    }

    this.codeState.set('checking');
    this.codeMessage.set(null);

    this.wbsService.validateCode(this.projectId(), code, this.chapter()?.id).subscribe({
      next: (validation) => {
        if (validation.isValid && validation.isAvailable) {
          this.codeState.set('valid');
          this.codeMessage.set(null);
          return;
        }

        this.codeState.set('invalid');
        this.codeMessage.set(
          validation.isValid
            ? 'Ese código ya está en uso en el proyecto (sin distinguir mayúsculas). Elige otro.'
            : 'El código debe tener entre 1 y 10 caracteres; se permiten letras, números y símbolos como . , - _.'
        );
      },
      error: (error: unknown) => {
        const apiError = resolveApiError(error);
        this.codeState.set('invalid');
        this.codeMessage.set(apiError.message);
      }
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const code = raw.code.trim();
    const name = raw.name.trim();
    const parentId = raw.parentChapterId || null;
    const chapter = this.chapter();

    this.saving.set(true);

    const request$ = chapter
      ? this.wbsService.updateChapter(this.projectId(), chapter.id, {
          code,
          name,
          rowVersion: chapter.rowVersion
        })
      : this.wbsService.createChapter(this.projectId(), {
          code,
          name,
          parentChapterId: parentId
        });

    request$.subscribe({
      next: (node) => {
        this.saving.set(false);
        this.visible.set(false);
        this.saved.emit(node);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        const apiError = resolveApiError(error);

        if (apiError.code === 'WBS_CODE_DUPLICATED' || apiError.code === 'WBS_CODE_INVALID') {
          this.codeState.set('invalid');
          this.codeMessage.set(apiError.message);
        }

        this.failed.emit(apiError);
      }
    });
  }
}
