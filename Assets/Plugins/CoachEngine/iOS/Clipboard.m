/*
 * Copyright (c) 2019 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

char *MakeStringCpy(const char* string);

void SetText_(const char* c){
    [UIPasteboard generalPasteboard].string = [NSString stringWithCString: c encoding:NSUTF8StringEncoding];
}

char *GetText_(){
    return MakeStringCpy([[UIPasteboard generalPasteboard].string UTF8String]);
}

char *MakeStringCpy(const char* string){
       if (string == NULL)
        return NULL;

    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}
