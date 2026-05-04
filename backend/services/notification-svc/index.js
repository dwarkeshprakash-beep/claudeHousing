const express = require('express');
const app = express();
app.use(express.json());
app.use('/notify', require('./routes/notify'));
app.listen(3001, () => console.log('Notification service on :3001'));
