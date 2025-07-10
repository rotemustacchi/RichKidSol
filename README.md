# RichKid - מערכת ניהול משתמשים (Fullstack - ASP.NET Core 9)

## 🧠 תיאור
מערכת לניהול משתמשים הכוללת:
- ממשק ניהול מבוסס ASP.NET MVC
- API חיצוני מבוסס ASP.NET Web API
- שמירת נתוני המשתמשים בקובץ JSON
- תמיכה מלאה בפעולות CRUD + סינון וחיפוש

---

## 🛠️ טכנולוגיות
- ASP.NET Core 9.0
- MVC (Model-View-Controller)
- Web API
- JSON כ־Data Store
- Visual Studio Code
- .NET CLI

---


## ▶️ הוראות הרצה

### שלב 0 (אופציונלי): בניית הפרויקטים

ניתן לבנות את שני הפרויקטים מראש בעזרת:

```bash
dotnet build RichKidSol.sln
```

או:

```bash
dotnet build RichKid.Web
dotnet build RichKid.API
```

> יש להריץ מתוך תיקיית `RichKidSol`

---

### שלב 1: פתיחת הפרויקט

- פתח את התיקייה `RichKidSol` ב־Visual Studio Code

---

### שלב 2: הרצה מתוך VS Code

1. עבור ללשונית השמאלית **Run and Debug** (סמל ▶️ עם חריץ)
2. בחר בקונפיגורציה:
   ```
   Start Web + API
   ```
3. לחץ ▶️ — וזהו! שני הפרויקטים יופעלו יחד (RichKid.Web + RichKid.API)

---

## 🌐 שימוש במערכת

### RichKid.Web – ממשק ניהול

> כתובת גישה: `https://localhost:7143/User`

כולל:
- טבלת משתמשים
- חיפוש לפי שם משתמש, טלפון, מייל
- סינון לפי סטטוס (פעיל / לא פעיל)
- הוספה, עריכה ומחיקה של משתמשים

---

### RichKid.API – Web API

כתובת בסיס: `https://localhost:7045` (או לפי מה שכתוב במסוף)

נקודות קצה (endpoints):

| Method | Endpoint                                     | תיאור |
|--------|----------------------------------------------|--------|
| GET    | `/api/users`                                 | החזרת כל המשתמשים |
| GET    | `/api/users/{id}`                            | משתמש לפי ID |
| GET    | `/api/users/search?firstName=X&lastName=Y`   | חיפוש לפי שם פרטי ומשפחה |
| POST   | `/api/users`                                 | יצירת משתמש חדש (JSON)

---

## 📝 הערות
- הקובץ `Users.json` משמש כבסיס נתונים לכל הפרויקט
- לא נדרש מסד נתונים חיצוני או התקנות נוספות
- הקוד נקי, מגיב ומופרד לשכבות (מודלים, שירותים, בקרים)

---

## 🧪 לבדיקה מהירה
- כנס ל־https://localhost:7143/User ← תראה טבלת משתמשים
- שלח קריאה ל־https://localhost:7045/api/users ← תראה JSON עם משתמשים

---

בהצלחה! 😄