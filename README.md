# RichKid – מערכת ניהול משתמשים (Fullstack ASP.NET Core 9)

## 🧠 תיאור
RichKid היא מערכת ניהול משתמשים מלאה, הכוללת:

- ממשק ניהול Web (ASP.NET MVC)
- API מאובטח (ASP.NET Web API + JWT)
- שמירת נתונים בקובץ JSON משותף
- תמיכה מלאה בפעולות CRUD
- סינון, חיפוש ואימות נתונים (Validation)

---

## 🛠️ טכנולוגיות

- ASP.NET Core 9.0
- MVC + Web API
- JSON כ־Data Store
- JWT Authentication
- Visual Studio Code / .NET CLI

---

## ▶️ הוראות הרצה

### 0️⃣ דרישות מקדימות

- מותקן [.NET 9 SDK](https://dotnet.microsoft.com/)
- Visual Studio Code (מומלץ)
- Git (אם ברצונך לשכפל)

---

### 1️⃣ פתיחת הפרויקט

- פתח את התיקייה `RichKidSol` ב־Visual Studio Code

---

### 2️⃣ Build ראשוני

אפשר להריץ:
```bash
dotnet build RichKidSol.sln
```

---

### 3️⃣ הרצת שני הפרויקטים (Web + API)

- עבור ללשונית `Run and Debug` ב־VS Code
- בחר קונפיגורציה: `Start Web + API`
- לחץ ▶️ והמערכת תעלה

---

## 🌐 שימוש במערכת

### 🖥️ RichKid.Web – ממשק ניהול

- כתובת: `https://localhost:7143/User`

כולל:
- טבלת משתמשים עם חיפוש + סינון סטטוס
- הוספה, עריכה, מחיקה עם ולידציות
- ניהול הרשאות לפי קבוצת משתמש
- התחברות + התנתקות (Session)

---

### 🔐 RichKid.API – Web API עם JWT

- כתובת בסיס: `https://localhost:7045`

#### 🔑 התחברות:
```http
POST /api/auth/login
```
Body:
```json
{
  "userName": "admin",
  "password": "admin123"
}
```
תחזיר טוקן מסוג JWT.

#### 🧪 שימוש עם הטוקן (Swagger או Postman)

- עבור כל Endpoint מוגן – השתמש ב־`Authorize` והדבק את הטוקן עם `Bearer <token>`

---

### 📡 Endpoints עיקריים:

| Method | Endpoint                                   | תיאור |
|--------|--------------------------------------------|--------|
| POST   | `/api/auth/login`                          | התחברות וקבלת טוקן |
| GET    | `/api/users`                               | כל המשתמשים |
| GET    | `/api/users/{id}`                          | לפי מזהה |
| GET    | `/api/users/search?firstName=X&lastName=Y` | חיפוש לפי שם |
| POST   | `/api/users`                               | יצירת משתמש |
| PUT    | `/api/users/{id}`                          | עדכון משתמש |
| DELETE | `/api/users/{id}`                          | מחיקת משתמש |

---

## 📁 נתונים

- כל נתוני המשתמשים נשמרים בקובץ `Users.json` (רלוונטי גם ל־API וגם ל־Web)

---

## 🧪 לבדיקה מהירה

- הרץ את המערכת
- התחבר עם משתמש קיים ב־Web
- התחבר עם JWT דרך Swagger (`/api/auth/login`)
- נסה קריאות API עם הטוקן

---

בהצלחה! 🚀