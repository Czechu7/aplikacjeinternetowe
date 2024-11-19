import { Component, inject } from '@angular/core';
import { AccountService } from '../../_services/account.service';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [],
  templateUrl: './products.component.html',
  styleUrl: './products.component.css'
})
export class ProductsComponent {
  accountService = inject(AccountService)
  products: any;


  ngOnInit() {
    this.accountService.getProducts().subscribe({
      next: products => this.products = products,
      error: error => console.log(error),
      complete: () => console.log('Request has been completed')
    });
  }

  addProduct() {
    this.accountService.addProduct(this.products).subscribe({
      next: products => this.products = products,
      error: error => console.log(error),
      complete: () => console.log('Request has been completed')
    });
  }
}
