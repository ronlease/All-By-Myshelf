import { Component, inject, OnInit, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RecordStore, StoreFinderService } from './store-finder.service';

type StoreType = 'records' | 'books';

@Component({
  selector: 'app-store-finder',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatButtonToggleModule,
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
export class StoreFinderComponent implements OnInit {
  error = signal(false);
  private readonly fb = inject(FormBuilder);
  loading = signal(false);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  searched = signal(false);
  private readonly storeFinderService = inject(StoreFinderService);
  stores = signal<RecordStore[]>([]);
  storeType = signal<StoreType>('records');

  get pageTitle(): string {
    return this.storeType() === 'records' ? 'Find Local Record Stores' : 'Find Local Bookstores';
  }

  locationForm = this.fb.group({
    location: ['', [Validators.required, this.usLocationValidator]],
  });

  ngOnInit(): void {
    const typeParam = this.route.snapshot.queryParamMap.get('type');
    if (typeParam === 'books') {
      this.storeType.set('books');
    }
  }

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

    const shopType = this.storeType() === 'records' ? 'music' : 'books';
    this.storeFinderService.findStores(location, shopType).subscribe({
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
