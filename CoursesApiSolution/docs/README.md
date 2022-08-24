# Courses API

## Resources

`/courses`

### GET /courses?category=Dance

```json
{
"numberOfCourses": 0,
"data": []

}

```

### GET /courses/{id}/instructor

```json
{
    "id": "1", 
    "title": "Web APIs",
    "numberOfhours": 27.5,
    "deliveryLocation": "Online",
    "instructor": {
        "id": "jeff",
        "name": "Jeff Gonzalez",
        "email": "jeff@hypertheory.com"
    }
}
```

`deliveryLocation` = `Online` | `InPerson` | 