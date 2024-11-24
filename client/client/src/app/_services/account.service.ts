import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { User } from '../_models/user';
import { map, Observable } from 'rxjs';
import { Products } from '../_models/products';
import { Cart } from '../_models/cart';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private http = inject(HttpClient);
  baseUrl = "http://localhost:5000/api/";
  currentUser = signal<User | null>(null)

  login(model: any) {
    return this.http.post<any>(this.baseUrl + "account/login", model).pipe(
      map(response => {
        if (response) {
          if (response.twoFactorRequired) {
            return { twoFactorRequired: true };
          }
          localStorage.setItem('user', JSON.stringify(response));
          this.currentUser.set(response);
          return response; 
        }
      })
    );
  }
  
  verifyTwoFactorCode(username: string, code: string): Observable<any> {
    return this.http.post(`${this.baseUrl}account/verify-2fa`, { code, username });
  }
  
  register(model: any){
    return this.http.post<User>(this.baseUrl + "account/register", model).pipe(
      map(user =>{
        if(user){
          localStorage.setItem('user', JSON.stringify(user))
          this.currentUser.set(user);
        }
        return user;
      })
    )
  }

  logout(){
    localStorage.removeItem('user');
    this.currentUser.set(null);
  }

  getUsers(){
    return this.http.get<User>(this.baseUrl + "users");
  }

  getProducts(){
    return this.http.get<Products>(this.baseUrl + "product")
  }

  addProduct(model: Products){
    return this.http.post<Products>(this.baseUrl + "product", model)
  }

  getCart(){
    return this.http.get<Cart>(this.baseUrl + "cart")
  }

  addCart(model: Cart){
    return this.http.post<Cart>(this.baseUrl + "cart", model)
  }

  getProductById(id: number) {
    return this.http.get<Products>(`${this.baseUrl}products/${id}`);
  }
  
  getCartById(id: number) {
    return this.http.get<Cart>(`${this.baseUrl}cart/${id}`);
  }

}
