import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RecordStore, StoreFinderService } from './store-finder.service';

@Component({
  selector: 'app-store-finder',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatToolbarModule,
    ReactiveFormsModule,
  ],
  templateUrl: './store-finder.component.html',
})
export class StoreFinderComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly storeFinderService = inject(StoreFinderService);

  error = signal(false);
  loading = signal(false);
  searched = signal(false);
  stores = signal<RecordStore[]>([]);

  locationForm = this.fb.group({
    location: ['', [Validators.required, this.usLocationValidator]],
  });

  onBackClick(): void {
    this.router.navigate(['/']);
  }

  onSubmit(): void {
    if (this.locationForm.invalid) {
      this.locationForm.markAllAsTouched();
      return;
    }

    const location = this.locationForm.value.location!;
    this.loading.set(true);
    this.error.set(false);
    this.searched.set(true);
    this.stores.set([]);

    this.storeFinderService.findStores(location).subscribe({
      next: (stores) => {
        this.stores.set(stores);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  private usLocationValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) return null;

    const zipPattern = /^\d{5}(-\d{4})?$/;
    const cityStatePattern = /^[a-zA-Z\s]+,\s*[A-Z]{2}$/i;

    if (zipPattern.test(control.value) || cityStatePattern.test(control.value)) {
      return null;
    }

    return { usLocationRequired: true };
  }
}
